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
        try
        {
            // Broadcast new player to all clients (including themselves)
            foreach (var recipient in clients.Values.Where(c => c.PlayerData.IsAuthenticated))
            {
                var packet = CreatePacket(29, buffer =>
                {
                    buffer.WriteBits(4, 1);  // Spawn mask
                    buffer.WriteBits(11, recipient == newPlayer ? 2047 : newPlayer.PlayerData.Index);
                    buffer.WriteString(newPlayer.PlayerData.Username);
                    buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.X);
                    buffer.WriteBits(16, (int)newPlayer.PlayerData.Position.Y);
                    buffer.WriteBits(3, newPlayer.PlayerData.Direction);
                    buffer.WriteBits(3, newPlayer.PlayerData.MovementType);
                });

                await recipient.GetStream().WriteAsync(packet).ConfigureAwait(false);
            }

            // Send existing players to new player
            foreach (var existingClient in clients.Values.Where(c => c.PlayerData.IsAuthenticated && c != newPlayer))
            {
                var packet = CreatePacket(29, buffer =>
                {
                    buffer.WriteBits(4, 1);  // Spawn mask
                    buffer.WriteBits(11, existingClient.PlayerData.Index);
                    buffer.WriteString(existingClient.PlayerData.Username);
                    buffer.WriteBits(16, (int)existingClient.PlayerData.Position.X);
                    buffer.WriteBits(16, (int)existingClient.PlayerData.Position.Y);
                    buffer.WriteBits(3, existingClient.PlayerData.Direction);
                    buffer.WriteBits(3, existingClient.PlayerData.MovementType);
                });

                await newPlayer.GetStream().WriteAsync(packet).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending player spawn: {ex.Message}");
        }
    }

    public async Task HandleLogout(GameClient disconnectedClient, ConcurrentDictionary<string, GameClient> clients)
    {
        try
        {
            var packet = CreatePacket(29, buffer =>
            {
                buffer.WriteBits(4, 2);  // Remove mask
                buffer.WriteBits(11, disconnectedClient.PlayerData.Index);
            });

            var tasks = clients.Values.Where(c => c.PlayerData.IsAuthenticated && c != disconnectedClient).Select(c => c.GetStream().WriteAsync(packet).AsTask());

            await Task.WhenAll(tasks);
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error broadcasting movement: {ex.Message}");
        }
    }
}