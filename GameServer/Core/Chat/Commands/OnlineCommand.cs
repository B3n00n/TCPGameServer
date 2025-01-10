using System.Collections.Concurrent;
using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class OnlineCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, GameClient> _clients;

        public OnlineCommand(ConcurrentDictionary<string, GameClient> clients)
        {
            _clients = clients;
        }

        public IEnumerable<string> Triggers => ["online", "players"];
        public int RequiredRank => 0;
        public string Description => "Shows the number of online players";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler chatPacketHandler)
        {
            int onlineCount = _clients.Count(x => x.Value.PlayerData.IsAuthenticated);
            await chatPacketHandler.SendGameMessage(sender, $"Users Online: {onlineCount}");
        }
    }
}