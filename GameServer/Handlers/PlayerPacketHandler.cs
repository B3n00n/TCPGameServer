using GameServer.Core.Network;
using GameServer.Domain.Models.Player;
using System.Collections.Concurrent;

public class PlayerPacketHandler
{
    private readonly BitBuffer headerBuffer;
    private readonly BitBuffer payloadBuffer;

    public PlayerPacketHandler()
    {
        headerBuffer = new BitBuffer(3);    // Fixed size for opcode + length
        payloadBuffer = new BitBuffer(256); // Typical payload size
    }

    private byte[] CreatePacket(byte opcode, Action<BitBuffer> writePayload)
    {
        payloadBuffer.Reset();
        writePayload(payloadBuffer);
        var payload = payloadBuffer.ToArray();

        headerBuffer.Reset();
        headerBuffer.WriteBits(8, opcode);
        headerBuffer.WriteBits(16, payload.Length);
        var header = headerBuffer.ToArray();

        var finalPacket = new byte[header.Length + payload.Length];
        Buffer.BlockCopy(header, 0, finalPacket, 0, header.Length);
        Buffer.BlockCopy(payload, 0, finalPacket, header.Length, payload.Length);

        return finalPacket;
    }

    public async Task SendPlayerSpawn(GameClient newPlayer, ConcurrentDictionary<string, GameClient> clients)
    {
        // Send spawn data
        foreach (var recipient in clients.Values.Where(c => c.PlayerData.IsAuthenticated))
        {
            var spawnPacket = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 1);  // Spawn mask
                buffer.WriteBits(11, recipient == newPlayer ? 2047 : newPlayer.PlayerData.Index);
                buffer.WriteString(newPlayer.PlayerData.Username);
                buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.X);
                buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.Y);
                buffer.WriteBits(3, newPlayer.PlayerData.Direction);
                buffer.WriteBits(3, newPlayer.PlayerData.MovementType);
            });

            await recipient.GetStream().WriteAsync(spawnPacket);
        }

        // Send existing players to new player
        foreach (var existingClient in clients.Values.Where(c => c.PlayerData.IsAuthenticated && c != newPlayer))
        {
            var packet = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 1);
                buffer.WriteBits(11, existingClient.PlayerData.Index);
                buffer.WriteString(existingClient.PlayerData.Username);
                buffer.WriteBits(16, (int)existingClient.PlayerData.Position.X);
                buffer.WriteBits(16, (int)existingClient.PlayerData.Position.Y);
                buffer.WriteBits(3, existingClient.PlayerData.Direction);
                buffer.WriteBits(3, existingClient.PlayerData.MovementType);
            });

            await newPlayer.GetStream().WriteAsync(packet);
        }

        // Send visual updates after spawns are complete
        await SendPlayerVisuals(newPlayer, clients);
    }

    public async Task SendPlayerVisuals(GameClient player, ConcurrentDictionary<string, GameClient> clients)
    {
        // Create visual packet for this player
        var visualsPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 4);  // Visuals mask
            buffer.WriteBits(11, 2047);  // Use 2047 when sending to themselves
            buffer.WriteBits(2, player.PlayerData.Direction);
            buffer.WriteBits(1, player.PlayerData.Gender);
            buffer.WriteBits(2, player.PlayerData.SkinTone);
            buffer.WriteBits(2, player.PlayerData.HairType);
            buffer.WriteBits(4, player.PlayerData.HairColor);
            buffer.WriteBits(16, player.PlayerData.HatId);
            buffer.WriteBits(16, player.PlayerData.TopId);
            buffer.WriteBits(16, player.PlayerData.LegsId);
        });

        // Always send to the player themselves first
        await player.GetStream().WriteAsync(visualsPacket);

        // Create packet for broadcasting to others (using their actual index)
        var broadcastPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 4);  // Visuals mask
            buffer.WriteBits(11, player.PlayerData.Index);  // Use actual index for other clients
            buffer.WriteBits(2, player.PlayerData.Direction);
            buffer.WriteBits(1, player.PlayerData.Gender);
            buffer.WriteBits(2, player.PlayerData.SkinTone);
            buffer.WriteBits(2, player.PlayerData.HairType);
            buffer.WriteBits(4, player.PlayerData.HairColor);
            buffer.WriteBits(16, player.PlayerData.HatId);
            buffer.WriteBits(16, player.PlayerData.TopId);
            buffer.WriteBits(16, player.PlayerData.LegsId);
        });

        // Then broadcast to other clients
        var tasks = clients.Values
            .Where(c => c.PlayerData.IsAuthenticated && c != player)
            .Select(c => c.GetStream().WriteAsync(broadcastPacket).AsTask());
        await Task.WhenAll(tasks);

        // Send all existing players' visuals to new player
        foreach (var existingClient in clients.Values.Where(c => c.PlayerData.IsAuthenticated && c != player))
        {
            var packet = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 4);
                buffer.WriteBits(11, existingClient.PlayerData.Index);
                buffer.WriteBits(2, existingClient.PlayerData.Direction);
                buffer.WriteBits(1, existingClient.PlayerData.Gender);
                buffer.WriteBits(2, existingClient.PlayerData.SkinTone);
                buffer.WriteBits(2, existingClient.PlayerData.HairType);
                buffer.WriteBits(4, existingClient.PlayerData.HairColor);
                buffer.WriteBits(16, existingClient.PlayerData.HatId);
                buffer.WriteBits(16, existingClient.PlayerData.TopId);
                buffer.WriteBits(16, existingClient.PlayerData.LegsId);
            });

            await player.GetStream().WriteAsync(packet);
        }
    }

    public async Task HandleLogout(GameClient disconnectedClient, ConcurrentDictionary<string, GameClient> clients)
    {
        var packet = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 2);  // Remove mask
            buffer.WriteBits(11, disconnectedClient.PlayerData.Index);
        });

        var tasks = clients.Values.Where(c => c.PlayerData.IsAuthenticated && c != disconnectedClient).Select(c => c.GetStream().WriteAsync(packet).AsTask());

        await Task.WhenAll(tasks);
    }

    public async Task HandleMovement(GameClient sourceClient, ConcurrentDictionary<string, GameClient> clients, PacketReader reader)
    {
        var x = await reader.ReadU16();
        var y = await reader.ReadU16();
        var direction = await reader.ReadU8();
        var movementType = await reader.ReadU8();

        sourceClient.PlayerData.Position = new Position(x, y);
        sourceClient.PlayerData.Direction = direction;
        sourceClient.PlayerData.MovementType = movementType;

        var packet = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 5);  // Movement mask
            buffer.WriteBits(11, sourceClient.PlayerData.Index);
            buffer.WriteBits(3, direction);
            buffer.WriteBits(16, x);
            buffer.WriteBits(16, y);
            buffer.WriteBits(3, movementType);
        });

        var tasks = clients.Values.Where(client => client.PlayerData.IsAuthenticated && client != sourceClient).Select(c => c.GetStream().WriteAsync(packet).AsTask());

        await Task.WhenAll(tasks);
    }
}