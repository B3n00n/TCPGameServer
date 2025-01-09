using System.Collections.Concurrent;
using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public class KickCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, GameClient> _clients;

        public KickCommand(ConcurrentDictionary<string, GameClient> clients)
        {
            _clients = clients;
        }

        public IEnumerable<string> Triggers => ["kick"];
        public int RequiredRank => 6;
        public string Description => "Kicks a player from the server";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
        {
            if (args.Length == 0) { await packetHandler.SendGameMessage(sender, "Usage: /kick <username> [reason]"); return; }

            string targetUsername = args[0];
            string reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "No reason provided";

            if (!_clients.TryGetValue(targetUsername, out GameClient? targetClient)) { await packetHandler.SendGameMessage(sender, $"{targetUsername} is not online."); return; }

            if (targetClient.PlayerData.Rank >= sender.PlayerData.Rank) { await packetHandler.SendGameMessage(sender, "You cannot kick your superiors."); return; }

            // Broadcast kick message only to staff members of equal or higher rank
            string kickMessage = $"<col=FF0000>{targetUsername} has been kicked by {sender.PlayerData.Username}. Reason: {reason}";
            var tasks = _clients.Values
                .Where(client => client.PlayerData.Rank >= sender.PlayerData.Rank)
                .Select(client => packetHandler.SendGameMessage(client, kickMessage));
            await Task.WhenAll(tasks);

            targetClient.Disconnect();
        }
    }
}