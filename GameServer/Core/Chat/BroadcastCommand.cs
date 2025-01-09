using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class BroadcastCommand : IChatCommand
    {
        public IEnumerable<string> Triggers => ["broadcast"];
        public int RequiredRank => 6;
        public string Description => "Broadcasts a message to all players";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler chatPacketHandler)
        {
            if (args.Length == 0) { await chatPacketHandler.SendGameMessage(sender, "Usage: /broadcast <message>"); return; }

            var message = string.Join(" ", args);

            await chatPacketHandler.BroadcastGameMessage($"<col=FF0000>[Broadcast] {message}");
        }
    }
}