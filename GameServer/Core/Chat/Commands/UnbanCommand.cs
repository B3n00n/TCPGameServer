using GameServer.Core.Chat;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;
using System.Collections.Concurrent;

public class UnbanCommand : IChatCommand
{
    private readonly ConcurrentDictionary<string, GameClient> _clients;
    private readonly AccountRepository _accountRepository;

    public UnbanCommand(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
    {
        _clients = clients;
        _accountRepository = accountRepository;
    }

    public IEnumerable<string> Triggers => ["unban"];
    public int RequiredRank => 6; // Admin rank
    public string Description => "Unbans a previously banned player";

    public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
    {
        if (args.Length == 0)
        {
            await packetHandler.SendGameMessage(sender, "Usage: /unban <username>");
            return;
        }

        string targetUsername = args[0];
        await _accountRepository.SetBanStatusAsync(targetUsername, false);

        // Notify staff
        string unbanMessage = $"{targetUsername} has been unbanned by {sender.PlayerData.Username}.";
        var tasks = _clients.Values
            .Where(client => client.PlayerData.Rank >= 4) // Only send to moderators and above
            .Select(client => packetHandler.SendGameMessage(client, unbanMessage));

        await Task.WhenAll(tasks);
    }
}