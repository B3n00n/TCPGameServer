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
        foreach (var recipient in clients.Values.Where(c => c.Data.IsAuthenticated))
        {
            var spawnPacket = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 1);  // Spawn mask
                buffer.WriteBits(11, recipient == newPlayer ? 2047 : newPlayer.Data.Index);
                buffer.WriteString(newPlayer.Data.Username);
                buffer.WriteBits(16, newPlayer.Data.Position.X);
                buffer.WriteBits(16, newPlayer.Data.Position.Y);
                buffer.WriteBits(3, newPlayer.Data.Direction);
                buffer.WriteBits(3, newPlayer.Data.MovementType);
            });

            await recipient.GetStream().WriteAsync(spawnPacket);
        }

        // Send existing players to new player
        foreach (var existingClient in clients.Values.Where(c => c.Data.IsAuthenticated && c != newPlayer))
        {
            var packet = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 1);
                buffer.WriteBits(11, existingClient.Data.Index);
                buffer.WriteString(existingClient.Data.Username);
                buffer.WriteBits(16, existingClient.Data.Position.X);
                buffer.WriteBits(16, existingClient.Data.Position.Y);
                buffer.WriteBits(3, existingClient.Data.Direction);
                buffer.WriteBits(3, existingClient.Data.MovementType);
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
            buffer.WriteBits(11, player.Data.Index);
            buffer.WriteBits(2, player.Data.Direction);
            buffer.WriteBits(1, player.Data.Gender);
            buffer.WriteBits(2, player.Data.SkinTone);
            buffer.WriteBits(2, player.Data.HairType);
            buffer.WriteBits(4, player.Data.HairColor);
            buffer.WriteBits(16, player.Data.HatId);
            buffer.WriteBits(16, player.Data.TopId);
            buffer.WriteBits(16, player.Data.LegsId);
            buffer.WriteBits(3, player.Data.MovementType);
        });

        // Create their "self view" packet (index 2047)
        var selfPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 4);  // Visuals mask
            buffer.WriteBits(11, 2047);
            buffer.WriteBits(2, player.Data.Direction);
            buffer.WriteBits(1, player.Data.Gender);
            buffer.WriteBits(2, player.Data.SkinTone);
            buffer.WriteBits(2, player.Data.HairType);
            buffer.WriteBits(4, player.Data.HairColor);
            buffer.WriteBits(16, player.Data.HatId);
            buffer.WriteBits(16, player.Data.TopId);
            buffer.WriteBits(16, player.Data.LegsId);
            buffer.WriteBits(3, player.Data.MovementType);
        });

        // Send player their own visuals and broadcast their visuals to others
        var tasks = new List<Task>();
        tasks.Add(player.GetStream().WriteAsync(selfPacket).AsTask());

        // Send other players' visuals to this player and broadcast this player's visuals
        foreach (var client in clients.Values)
        {
            if (client != player && client.Data.IsAuthenticated)
            {
                // Send this player's visuals to other client
                tasks.Add(client.GetStream().WriteAsync(broadcastPacket).AsTask());

                // Send other client's visuals to this player
                var packet = CreatePacket(29, buffer =>
                {
                    buffer.WriteBits(4, 4);
                    buffer.WriteBits(11, client.Data.Index);
                    buffer.WriteBits(2, client.Data.Direction);
                    buffer.WriteBits(1, client.Data.Gender);
                    buffer.WriteBits(2, client.Data.SkinTone);
                    buffer.WriteBits(2, client.Data.HairType);
                    buffer.WriteBits(4, client.Data.HairColor);
                    buffer.WriteBits(16, client.Data.HatId);
                    buffer.WriteBits(16, client.Data.TopId);
                    buffer.WriteBits(16, client.Data.LegsId);
                    buffer.WriteBits(3, client.Data.MovementType);
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
            buffer.WriteBits(11, disconnectedClient.Data.Index);
        });

        var tasks = clients.Values.Where(c => c.Data.IsAuthenticated && c != disconnectedClient).Select(c => c.GetStream().WriteAsync(packet).AsTask());

        await Task.WhenAll(tasks);
    }

    public async Task HandleMovement(GameClient sourceClient, ConcurrentDictionary<string, GameClient> clients, PacketReader reader)
    {
        var x = await reader.ReadU16();
        var y = await reader.ReadU16();
        var direction = await reader.ReadU8();
        var movementType = await reader.ReadU8();

        if (sourceClient.Data.State != null)
        {
            sourceClient.Data.State.PositionX = x;
            sourceClient.Data.State.PositionY = y;
            sourceClient.Data.State.Direction = direction;
            sourceClient.Data.State.MovementType = movementType;
        }

        var packet = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 5);  // Movement mask
            buffer.WriteBits(11, sourceClient.Data.Index);
            buffer.WriteBits(3, direction);
            buffer.WriteBits(16, x);
            buffer.WriteBits(16, y);
            buffer.WriteBits(3, movementType);
        });

        var tasks = clients.Values.Where(client => client.Data.IsAuthenticated && client != sourceClient).Select(c => c.GetStream().WriteAsync(packet).AsTask());

        await Task.WhenAll(tasks);
    }

    public async Task BroadcastChatBubble(GameClient sourceClient, string message, ConcurrentDictionary<string, GameClient> clients)
    {
        var packet = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 8);  // Chat bubble mask
            buffer.WriteBits(11, sourceClient.Data.Index);
            buffer.WriteString(message);
        });

        var tasks = clients.Values
            .Where(client => client.Data.IsAuthenticated && client != sourceClient)
            .Select(c => c.GetStream().WriteAsync(packet).AsTask());

        // Send to the source client with index 2047
        var selfPacket = CreatePacket(29, buffer =>
        {
            buffer.WriteBits(4, 8);  // Chat bubble mask
            buffer.WriteBits(11, 2047); // Self index
            buffer.WriteString(message);
        });

        tasks = tasks.Append(sourceClient.GetStream().WriteAsync(selfPacket).AsTask());

        await Task.WhenAll(tasks);
    }
}