using GameServer.Packets;
using GameServer;
using System.Collections.Concurrent;
using System.Text;

public class PlayerPacketHandler
{
    public async Task SendPlayerSpawn(GameClient newPlayer, ConcurrentDictionary<string, GameClient> clients)
    {
        try
        {
            // First, tell all existing players about the new player
            var spawnPacket = new PacketWriter();
            spawnPacket.WriteU8(29);  // Opcode

            // Correctly calculate the length
            // 2 (mask) + 2 (index) + 4 (username length) + username length + 2 (x) + 2 (y) + 1 (direction)
            var usernameBytes = Encoding.UTF8.GetBytes(newPlayer.PlayerData.Username);
            var packetLength = (ushort)(2 + 2 + 4 + usernameBytes.Length + 2 + 2 + 1);

            spawnPacket.WriteU16(packetLength);
            spawnPacket.WriteU16(1);         // Spawn mask
            spawnPacket.WriteU16((ushort)newPlayer.PlayerData.Index);

            // Write username length as U32 and then username bytes
            spawnPacket.WriteString(newPlayer.PlayerData.Username);

            spawnPacket.WriteU16((ushort)newPlayer.PlayerData.Position.X);
            spawnPacket.WriteU16((ushort)newPlayer.PlayerData.Position.Y);
            spawnPacket.WriteU8(newPlayer.PlayerData.Direction);

            var packetData = spawnPacket.ToArray();

            foreach (var client in clients.Values)
            {
                if (client != newPlayer && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(packetData);
                }
            }

            // Then, tell the new player about all existing players
            foreach (var existingPlayer in clients.Values)
            {
                if (existingPlayer != newPlayer && existingPlayer.PlayerData.IsAuthenticated)
                {
                    // Send spawn packet first
                    var existingPlayerSpawn = new PacketWriter();
                    existingPlayerSpawn.WriteU8(29);

                    var existingUsernameBytes = Encoding.UTF8.GetBytes(existingPlayer.PlayerData.Username);
                    var existingPacketLength = (ushort)(2 + 2 + 4 + existingUsernameBytes.Length + 2 + 2 + 1);

                    existingPlayerSpawn.WriteU16(existingPacketLength);
                    existingPlayerSpawn.WriteU16(1);  // Spawn mask
                    existingPlayerSpawn.WriteU16((ushort)existingPlayer.PlayerData.Index);

                    // Write username length as U32 and then username bytes
                    existingPlayerSpawn.WriteString(existingPlayer.PlayerData.Username);

                    existingPlayerSpawn.WriteU16((ushort)existingPlayer.PlayerData.Position.X);
                    existingPlayerSpawn.WriteU16((ushort)existingPlayer.PlayerData.Position.Y);
                    existingPlayerSpawn.WriteU8(existingPlayer.PlayerData.Direction);

                    await newPlayer.GetStream().WriteAsync(existingPlayerSpawn.ToArray());

                    // Then immediately send a movement update with current position
                    var movementUpdate = new PacketWriter();
                    movementUpdate.WriteU8(29);

                    // Calculate movement update packet length
                    var movementPacketLength = (ushort)(2 + 2 + 1 + 2 + 2 + 1);
                    movementUpdate.WriteU16(movementPacketLength);

                    movementUpdate.WriteU16(5);  // Movement update mask
                    movementUpdate.WriteU16((ushort)existingPlayer.PlayerData.Index);
                    movementUpdate.WriteU8(existingPlayer.PlayerData.Direction);
                    movementUpdate.WriteU16((ushort)existingPlayer.PlayerData.Position.X);
                    movementUpdate.WriteU16((ushort)existingPlayer.PlayerData.Position.Y);
                    movementUpdate.WriteU8(existingPlayer.PlayerData.MovementType);

                    await newPlayer.GetStream().WriteAsync(movementUpdate.ToArray());
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
            // Send player removal packet to all other clients
            var packet = new PacketWriter();
            packet.WriteU8(29);         // Player update opcode
            packet.WriteU16(4);         // Length: mask(2) + index(2)
            packet.WriteU16(2);         // Remove player mask
            packet.WriteU16((ushort)sourceClient.PlayerData.Index);

            var packetData = packet.ToArray();
            Console.WriteLine($"Sending player removal packet - Index: {sourceClient.PlayerData.Index}");

            foreach (var client in clients.Values)
            {
                if (client != sourceClient && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(packetData);
                }
            }

            // Remove from clients dictionary if present
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

            // Broadcast movement to other clients
            var response = new PacketWriter();
            response.WriteU8(29);         // Player update opcode
            response.WriteU16(10);        // Payload length
            response.WriteU16(5);         // Movement mask
            response.WriteU16((ushort)sourceClient.PlayerData.Index);
            response.WriteU8(direction);
            response.WriteU16(x);
            response.WriteU16(y);
            response.WriteU8(movementType);

            foreach (var client in clients.Values)
            {
                if (client != sourceClient && client.PlayerData.IsAuthenticated)
                {
                    await client.GetStream().WriteAsync(response.ToArray());
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error broadcasting movement: {ex.Message}");
        }
    }
}