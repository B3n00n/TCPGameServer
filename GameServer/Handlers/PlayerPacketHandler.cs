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
        // Create one packet for broadcasting this player's visuals
        var broadcastPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 4);  // Visuals mask
            buffer.WriteBits(11, player.PlayerData.Index);
            buffer.WriteBits(2, player.PlayerData.Direction);
            buffer.WriteBits(1, player.PlayerData.Gender);
            buffer.WriteBits(2, player.PlayerData.SkinTone);
            buffer.WriteBits(2, player.PlayerData.HairType);
            buffer.WriteBits(4, player.PlayerData.HairColor);
            buffer.WriteBits(16, player.PlayerData.HatId);
            buffer.WriteBits(16, player.PlayerData.TopId);
            buffer.WriteBits(16, player.PlayerData.LegsId);
            buffer.WriteBits(3, player.PlayerData.MovementType);
        });

        // Create their "self view" packet (index 2047)
        var selfPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 4);  // Visuals mask
            buffer.WriteBits(11, 2047);
            buffer.WriteBits(2, player.PlayerData.Direction);
            buffer.WriteBits(1, player.PlayerData.Gender);
            buffer.WriteBits(2, player.PlayerData.SkinTone);
            buffer.WriteBits(2, player.PlayerData.HairType);
            buffer.WriteBits(4, player.PlayerData.HairColor);
            buffer.WriteBits(16, player.PlayerData.HatId);
            buffer.WriteBits(16, player.PlayerData.TopId);
            buffer.WriteBits(16, player.PlayerData.LegsId);
            buffer.WriteBits(3, player.PlayerData.MovementType);
        });

        // Send player their own visuals and broadcast their visuals to others
        var tasks = new List<Task>();
        tasks.Add(player.GetStream().WriteAsync(selfPacket).AsTask());

        // Send other players' visuals to this player and broadcast this player's visuals
        foreach (var client in clients.Values)
        {
            if (client != player && client.PlayerData.IsAuthenticated)
            {
                // Send this player's visuals to other client
                tasks.Add(client.GetStream().WriteAsync(broadcastPacket).AsTask());

                // Send other client's visuals to this player
                var packet = CreatePacket(29, buffer =>
                {
                    buffer.WriteBits(4, 4);
                    buffer.WriteBits(11, client.PlayerData.Index);
                    buffer.WriteBits(2, client.PlayerData.Direction);
                    buffer.WriteBits(1, client.PlayerData.Gender);
                    buffer.WriteBits(2, client.PlayerData.SkinTone);
                    buffer.WriteBits(2, client.PlayerData.HairType);
                    buffer.WriteBits(4, client.PlayerData.HairColor);
                    buffer.WriteBits(16, client.PlayerData.HatId);
                    buffer.WriteBits(16, client.PlayerData.TopId);
                    buffer.WriteBits(16, client.PlayerData.LegsId);
                    buffer.WriteBits(3, client.PlayerData.MovementType);
                });
                tasks.Add(player.GetStream().WriteAsync(packet).AsTask());
            }
        }

        await Task.WhenAll(tasks);
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