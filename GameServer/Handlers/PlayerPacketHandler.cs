using GameServer;
using GameServer.Packets;
using System.Collections.Concurrent;

public class PlayerPacketHandler
{
    private readonly BitBuffer headerBuffer;
    private readonly BitBuffer payloadBuffer;
    private readonly byte[] finalPacket;

    public PlayerPacketHandler()
    {
        headerBuffer = new BitBuffer(3);    // Fixed size for opcode + length
        payloadBuffer = new BitBuffer(256); // Typical payload size
        finalPacket = new byte[1024];       // Max packet size
    }

    private int CreatePacket(byte opcode, Action<BitBuffer> writePayload)
    {
        payloadBuffer.Reset();
        writePayload(payloadBuffer);
        var payload = payloadBuffer.ToArray();

        headerBuffer.Reset();
        headerBuffer.WriteBits(8, opcode);
        headerBuffer.WriteBits(16, payload.Length);
        var header = headerBuffer.ToArray();

        Buffer.BlockCopy(header, 0, finalPacket, 0, header.Length);
        Buffer.BlockCopy(payload, 0, finalPacket, header.Length, payload.Length);

        return header.Length + payload.Length;
    }

    public async Task SendPlayerSpawn(GameClient newPlayer, ConcurrentDictionary<string, GameClient> clients)
    {
        try
        {
            // Send existing players to new player
            foreach (var existingClient in clients.Values)
            {
                if (existingClient != newPlayer && existingClient.PlayerData.IsAuthenticated)
                {
                    int packetSize = CreatePacket(29, buffer =>
                    {
                        buffer.WriteBits(4, 1);  // Spawn mask
                        buffer.WriteBits(11, existingClient.PlayerData.Index);
                        buffer.WriteString(existingClient.PlayerData.Username);
                        buffer.WriteBits(16, (int)existingClient.PlayerData.Position.X);
                        buffer.WriteBits(16, (int)existingClient.PlayerData.Position.Y);
                        buffer.WriteBits(3, existingClient.PlayerData.Direction);
                    });

                    await newPlayer.GetStream().WriteAsync(finalPacket, 0, packetSize);
                }
            }

            // Broadcast new player to others
            int newPlayerPacketSize = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 1);  // Spawn mask
                buffer.WriteBits(11, newPlayer.PlayerData.Index);
                buffer.WriteString(newPlayer.PlayerData.Username);
                buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.X);
                buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.Y);
                buffer.WriteBits(3, newPlayer.PlayerData.Direction);
            });

            foreach (var client in clients.Values)
            {
                if (client != newPlayer && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(finalPacket, 0, newPlayerPacketSize);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending player spawn: {ex.Message}");
        }
    }

    public async Task HandleLogout(GameClient sourceClient, ConcurrentDictionary<string, GameClient> clients)
    {
        try
        {
            int packetSize = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 2);  // Remove mask
                buffer.WriteBits(11, sourceClient.PlayerData.Index);
            });

            foreach (var client in clients.Values)
            {
                if (client != sourceClient && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(finalPacket, 0, packetSize);
                }
            }

            if (!string.IsNullOrEmpty(sourceClient.PlayerData.Username))
            {
                clients.TryRemove(sourceClient.PlayerData.Username, out _);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling player logout: {ex.Message}");
        }
    }

    public async Task HandleMovement(GameClient sourceClient, ConcurrentDictionary<string, GameClient> clients, PacketReader reader)
    {
        try
        {
            var x = await reader.ReadU16();
            var y = await reader.ReadU16();
            var direction = await reader.ReadU8();
            var movementType = await reader.ReadU8();

            sourceClient.PlayerData.Position = new Position(x, y);
            sourceClient.PlayerData.Direction = direction;
            sourceClient.PlayerData.MovementType = movementType;

            int packetSize = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 5);  // Movement mask
                buffer.WriteBits(11, sourceClient.PlayerData.Index);
                buffer.WriteBits(3, direction);
                buffer.WriteBits(16, x);
                buffer.WriteBits(16, y);
                buffer.WriteBits(3, movementType);
            });

            foreach (var client in clients.Values)
            {
                if (client != sourceClient && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(finalPacket, 0, packetSize);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error broadcasting movement: {ex.Message}");
        }
    }
}