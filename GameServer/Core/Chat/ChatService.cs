using System.Collections.Concurrent;
using GameServer.Core.Events;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;

namespace GameServer.Core.Chat
{
    public class ChatService
    {
        private readonly ChatPacketHandler _chatPacketHandler;
        private readonly CommandHandler _commandHandler;

        public ChatService(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
        {
            _chatPacketHandler = new ChatPacketHandler(clients);
            _commandHandler = new CommandHandler(clients, accountRepository);
        }

        public async Task HandleChat(GameClient sender, PacketReader reader)
        {
            var message = await _chatPacketHandler.ReadMessage(sender, reader);
            if (message == null) return;

            await HandleIncomingMessage(sender, message);
        }

        private async Task HandleIncomingMessage(GameClient sender, string message)
        {
            if (message.StartsWith("/"))
            {
                string[] commandParts = message[1..].Split(' ');
                await _commandHandler.HandleCommand(sender, commandParts, _chatPacketHandler);
                return;
            }

            await _chatPacketHandler.BroadcastChatMessage(sender, message);

            ChatEvents.OnChatMessageSent?.Invoke(sender, message);
        }
    }
}