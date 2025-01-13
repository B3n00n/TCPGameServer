using System.Collections.Concurrent;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;

namespace GameServer.Core.Chat
{
    public class MuteCommand : IChatCommand
    {
        private readonly ConcurrentDictionary<string, GameClient> _clients;
        private readonly AccountRepository _accountRepository;

        public MuteCommand(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
        {
            _clients = clients;
            _accountRepository = accountRepository;
        }

        public IEnumerable<string> Triggers => ["mute"];
        public int RequiredRank => 4;
        public string Description => "Mutes a player preventing them from chatting";

        public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
        {
            if (args.Length == 0)
            {
                await packetHandler.SendGameMessage(sender, "Usage: /mute <username> [reason]");
                return;
            }

            string targetUsername = args[0];
            string reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "No reason provided";

            var targetAccount = await _accountRepository.GetByUsernameAsync(targetUsername);
            if (targetAccount == null)
            {
                await packetHandler.SendGameMessage(sender, $"Player {targetUsername} not found.");
                return;
            }

            if (targetAccount.Rank >= sender.PlayerData.Rank)
            {
                await packetHandler.SendGameMessage(sender, "You cannot mute players of equal or higher rank.");
                return;
            }

            await _accountRepository.SetMuteStatusAsync(targetUsername, true);

            if (_clients.TryGetValue(targetUsername, out var targetClient))
            {
                targetClient.PlayerData.IsMuted = true;
                await packetHandler.SendGameMessage(targetClient, "You have been muted.");
            }

            string muteMessage = $"<col=FF0000>{targetUsername} has been muted by {sender.PlayerData.Username}. Reason: {reason}";
            var tasks = _clients.Values
                .Where(client => client.PlayerData.Rank >= 4)
                .Select(client => packetHandler.SendGameMessage(client, muteMessage));

            await Task.WhenAll(tasks);
        }
    }
}