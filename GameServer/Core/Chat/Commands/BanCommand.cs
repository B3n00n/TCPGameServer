using GameServer.Core.Chat;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;
using System.Collections.Concurrent;

public class BanCommand : IChatCommand
{
    private readonly ConcurrentDictionary<string, GameClient> _clients;
    private readonly AccountRepository _accountRepository;

    public BanCommand(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
    {
        _clients = clients;
        _accountRepository = accountRepository;
    }

    public IEnumerable<string> Triggers => ["ban"];
    public int RequiredRank => 6; // Admin rank
    public string Description => "Bans a player from the server";

    public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
    {
        if (args.Length == 0)
        {
            await packetHandler.SendGameMessage(sender, "Usage: /ban <username> [reason]");
            return;
        }

        string targetUsername = args[0];
        string reason = args.Length > 1 ? string.Join(" ", args.Skip(1)) : "No reason provided";

        // Get target account to check rank
        var targetAccount = await _accountRepository.GetByUsernameAsync(targetUsername);
        if (targetAccount == null)
        {
            await packetHandler.SendGameMessage(sender, $"Player {targetUsername} not found.");
            return;
        }

        // Check rank
        if (targetAccount.Rank >= sender.PlayerData.Rank)
        {
            await packetHandler.SendGameMessage(sender, "You cannot ban players of equal or higher rank.");
            return;
        }

        await _accountRepository.SetBanStatusAsync(targetUsername, true);

        // Broadcast ban message to all players
        string banMessage = $"<col=FF0000>{targetUsername} has been banned by {sender.PlayerData.Username}. Reason: {reason}";
        await packetHandler.BroadcastGameMessage(banMessage);

        // Disconnect if online
        if (_clients.TryGetValue(targetUsername, out var targetClient))
        {
            targetClient.Disconnect();
        }
    }
}