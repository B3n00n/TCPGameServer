using System.Collections.Concurrent;
using GameServer.Core.Network;
using System.Text.RegularExpressions;
using System.Text;

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
                var length = await reader.ReadU16();
                if (length <= 0 || length > MAX_MESSAGE_LENGTH) return;

                var buffer = new byte[length];
                await sourceClient.GetStream().ReadAsync(buffer, 0, length);
                var message = _sanitizePattern.Replace(Encoding.ASCII.GetString(buffer).Trim(), string.Empty);

                if (string.IsNullOrEmpty(message)) return;

                var writer = new PacketWriter();
                writer.WriteU8(4); // Chat opcode

                // Calculate total length (4 for string lengths + username + message + 1 for rank)
                var totalLength = 4 + Encoding.ASCII.GetByteCount(sourceClient.PlayerData.Username) + 4 + Encoding.ASCII.GetByteCount(message) + 1;
                writer.WriteU16((ushort)totalLength);

                writer.WriteString(sourceClient.PlayerData.Username);
                writer.WriteString(message);
                writer.WriteU8(sourceClient.PlayerData.Rank);

                var responseData = writer.ToArray();

                // Broadcast to all authenticated clients except sender
                var tasks = new List<ValueTask>();
                foreach (var client in clients.Values)
                {
                    if (client.PlayerData.IsAuthenticated && client != sourceClient)
                    {
                        tasks.Add(client.GetStream().WriteAsync(responseData));
                    }
                }
                foreach (var task in tasks)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error: {ex.Message}");
            }
        }

        public async Task SendGameMessage(GameClient client, string message)
        {
            try
            {
                if (!client.PlayerData.IsAuthenticated) return;

                var writer = new PacketWriter();
                writer.WriteU8(6);  // Game message opcode

                var sanitizedMessage = _sanitizePattern.Replace(message.Trim(), string.Empty);
                if (string.IsNullOrEmpty(sanitizedMessage)) return;

                var totalLength = 4 + Encoding.ASCII.GetByteCount(sanitizedMessage);
                writer.WriteU16((ushort)totalLength);
                writer.WriteString(sanitizedMessage);

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
                var sanitizedMessage = _sanitizePattern.Replace(message.Trim(), string.Empty);
                if (string.IsNullOrEmpty(sanitizedMessage)) return;

                var writer = new PacketWriter();
                writer.WriteU8(6);  // Game message opcode

                var totalLength = 4 + Encoding.ASCII.GetByteCount(sanitizedMessage);
                writer.WriteU16((ushort)totalLength);
                writer.WriteString(sanitizedMessage);

                var responseData = writer.ToArray();

                var tasks = new List<ValueTask>();
                foreach (var client in clients.Values)
                {
                    if (client.PlayerData.IsAuthenticated)
                    {
                        tasks.Add(client.GetStream().WriteAsync(responseData));
                    }
                }
                foreach (var task in tasks)
                {
                    await task;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Chat] Error broadcasting game message: {ex.Message}");
            }
        }
    }
}