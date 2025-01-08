using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class HelpCommand : IChatCommand
    {
        private readonly Dictionary<int, string> _rankHelpMessages;

        public HelpCommand(Dictionary<int, string> rankHelpMessages)
        {
            _rankHelpMessages = rankHelpMessages;
        }

        public IEnumerable<string> Triggers => ["help", "commands"];
        public int RequiredRank => 0;
        public string Description => "Shows available commands";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
        {
            await packetHandler.SendGameMessage(sender, _rankHelpMessages[sender.PlayerData.Rank]);
        }
    }
}