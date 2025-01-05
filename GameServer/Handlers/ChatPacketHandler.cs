using System.Collections.Concurrent;
using GameServer.Core.Network;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GameServer.Handlers
{
    public class ChatPacketHandler
    {
        private const int MAX_MESSAGE_LENGTH = 150;
        private readonly Regex _sanitizePattern = new(@"[^\u0020-\u007E]", RegexOptions.Compiled);

        public async Task HandleChat(GameClient sourceClient, ConcurrentDictionary<string, GameClient> clients, PacketReader reader)
        {
            try
            {
                // Read message length first (16-bit)
                var length = await reader.ReadU16();
                var buffer = new byte[length];
                await sourceClient.GetStream().ReadAsync(buffer, 0, length);
                var message = System.Text.Encoding.UTF8.GetString(buffer);

                Console.WriteLine($"[Chat] Received message from {sourceClient.PlayerData.Username}: {message}");

                if (string.IsNullOrWhiteSpace(message) || message.Length > MAX_MESSAGE_LENGTH)
                {
                    return;
                }

                // Trim and sanitize message
                message = message.Trim();
                message = _sanitizePattern.Replace(message, string.Empty);

                if (message.Length == 0)
                {
                    return;
                }

                // Create the chat packet
                var writer = new PacketWriter();
                writer.WriteU8(4); // Chat opcode

                // Use the built-in WriteString method which matches client's reading format
                writer.WriteString(sourceClient.PlayerData.Username);
                writer.WriteString(message);
                writer.WriteU8(sourceClient.PlayerData.Rank);

                var responseData = writer.ToArray();
                Console.WriteLine($"[Chat Debug] Packet length: {responseData.Length}");
                Console.WriteLine($"[Chat Debug] Raw bytes: {BitConverter.ToString(responseData)}");

                // Broadcast to all connected and authenticated clients
                foreach (var client in clients.Values)
                {
                    if (client.PlayerData.IsAuthenticated)
                    {
                        await client.GetStream().WriteAsync(responseData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error handling chat message: {ex.Message}");
                Console.WriteLine($"[Chat] Stack trace: {ex.StackTrace}");
            }
        }

        public async Task SendGameMessage(GameClient client, string message)
        {
            try
            {
                var writer = new PacketWriter();
                writer.WriteU8(6);  // Game message opcode
                writer.WriteString(message);
                await client.GetStream().WriteAsync(writer.ToArray());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error sending game message: {ex.Message}");
            }
        }

        public async Task BroadcastGameMessage(ConcurrentDictionary<string, GameClient> clients, string message)
        {
            try
            {
                var writer = new PacketWriter();
                writer.WriteU8(6);  // Game message opcode
                writer.WriteString(message);
                var responseData = writer.ToArray();

                foreach (var client in clients.Values)
                {
                    if (client.PlayerData.IsAuthenticated)
                    {
                        await client.GetStream().WriteAsync(responseData);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error broadcasting game message: {ex.Message}");
            }
        }
    }
}