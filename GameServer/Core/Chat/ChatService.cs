using System.Collections.Concurrent;
using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class ChatService
    {
        private readonly ChatPacketHandler _packetHandler;
        private readonly CommandHandler _commandHandler;

        public ChatService(ConcurrentDictionary<string, GameClient> clients)
        {
            _packetHandler = new ChatPacketHandler(clients);
            _commandHandler = new CommandHandler(clients);
        }

        public async Task HandlePacket(GameClient sender, PacketReader reader)
        {
            var message = await _packetHandler.ReadMessage(sender, reader);
            if (message == null) return;

            await HandleIncomingMessage(sender, message);
        }

        private async Task HandleIncomingMessage(GameClient sender, string message)
        {
            if (message.StartsWith("/"))
            {
                string[] commandParts = message[1..].Split(' ');
                await _commandHandler.HandleCommand(sender, commandParts, _packetHandler);
                return;
            }

            await _packetHandler.BroadcastChatMessage(sender, message);
        }
    }
}