using GameServer.Core.Chat;
using GameServer.Core.Network;
using GameServer.Handlers;
using GameServer.Infrastructure.Repositories;
using System.Collections.Concurrent;

public class UnmuteCommand : IChatCommand
{
    private readonly ConcurrentDictionary<string, GameClient> _clients;
    private readonly AccountRepository _accountRepository;

    public UnmuteCommand(ConcurrentDictionary<string, GameClient> clients, AccountRepository accountRepository)
    {
        _clients = clients;
        _accountRepository = accountRepository;
    }

    public IEnumerable<string> Triggers => ["unmute"];
    public int RequiredRank => 6;
    public string Description => "Unmutes a previously muted player";

    public async Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler)
    {
        if (args.Length == 0) { await packetHandler.SendGameMessage(sender, "Usage: /unmute <username>"); return; }

        string targetUsername = args[0];

        var targetAccount = await _accountRepository.GetByUsernameAsync(targetUsername);
        if (targetAccount == null) { await packetHandler.SendGameMessage(sender, $"Player {targetUsername} not found."); return; }

        await _accountRepository.SetMuteStatusAsync(targetUsername, false);

        if (_clients.TryGetValue(targetUsername, out var targetClient))
        {
            targetClient.PlayerData.IsMuted = false;
            await packetHandler.SendGameMessage(targetClient, "You have been unmuted.");
        }

        string unmuteMessage = $"{targetUsername} has been unmuted by {sender.PlayerData.Username}.";
        var tasks = _clients.Values.Where(client => client.PlayerData.Rank >= RequiredRank).Select(client => packetHandler.SendGameMessage(client, unmuteMessage));

        await Task.WhenAll(tasks);
    }
}