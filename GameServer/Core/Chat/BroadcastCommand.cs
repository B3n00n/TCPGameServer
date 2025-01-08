using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class BroadcastCommand : IChatCommand
    {
        public IEnumerable<string> Triggers => ["help", "commands"];
        public int RequiredRank => 6;
        public string Description => "Shows available commands";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
        {
            await packetHandler.BroadcastChatMessage(sender, "MESSAGE!");
        }
    }
}