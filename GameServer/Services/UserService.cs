using GameServer.Core.Network;
using GameServer.Domain.Models;
using GameServer.Handlers;
using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Database;
using GameServer.Infrastructure.Repositories;
using System.Collections.Concurrent;

public class UserService
{
    private readonly AccountRepository _accountRepo;
    private readonly AccountStateRepository _stateRepo;

    public UserService(DatabaseContext db)
    {
        _accountRepo = new AccountRepository(db);
        _stateRepo = new AccountStateRepository(db);
    }

    public async Task<(LoginType Status, Account? Account, AccountState? State)> AuthenticateAsync(string username, string password, uint revision, ConcurrentDictionary<string, GameClient> activeClients)
    {
        if (revision != GameConfig.REVISION)
            return (LoginType.REVISION_MISMATCH, null, null);

        if (activeClients.ContainsKey(username))
            return (LoginType.ALREADY_ONLINE, null, null);

        var account = await _accountRepo.GetByUsernameAsync(username);

        if (account == null) return (LoginType.UNUSED, null, null);


        if (account.IsBanned) return (LoginType.ACCOUNT_BANNED, null, null);


        if (password != account.Password) return (LoginType.INVALID_CREDENTIALS, null, null);


        var state = await _stateRepo.GetByAccountIdAsync(account.Id);

        if (state == null) return (LoginType.COULD_NOT_COMPLETE_LOGIN, null, null);

        await _accountRepo.UpdateLastLoginAsync(account.Id);

        return (LoginType.ACCEPTABLE, account, state);
    }

    public async Task SaveUserDataAsync(int accountId, int x, int y, byte direction, byte movementType)
    {
        await _stateRepo.UpdatePositionAsync(accountId, x, y, direction, movementType);
    }
}