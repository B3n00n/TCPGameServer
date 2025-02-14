using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class BattleCommand : IChatCommand
    {
        public IEnumerable<string> Triggers => new[] { "battle" };
        public int RequiredRank => 0; // All players can battle
        public string Description => "Initiates a PoC Pokemon battle";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler chatPacketHandler)
        {
            await chatPacketHandler.SendBattleInitiation(sender);
            await chatPacketHandler.SendGameMessage(sender, "Initiating battle...");
        }
    }
}