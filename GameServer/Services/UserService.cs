using GameServer.Core.Network;
using GameServer.Domain.Models;
using GameServer.Handlers;
using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Repositories;
using System.Collections.Concurrent;

public class UserService
{
    private readonly AccountRepository _accountRepository;
    private readonly AccountStateRepository _stateRepository;
    private readonly AccountVisualsRepository _visualsRepository;

    public UserService(AccountRepository accountRepo, AccountStateRepository stateRepository, AccountVisualsRepository visualsRepository)
    {
        _accountRepository = accountRepo;
        _stateRepository = stateRepository;
        _visualsRepository = visualsRepository;
    }

    public async Task<(LoginType Status, Account? Account, AccountState? State, AccountVisuals? Visuals)> AuthenticateAsync(string username, string password, uint revision, ConcurrentDictionary<string, GameClient> activeClients)
    {
        if (revision != GameConfig.REVISION)
            return (LoginType.REVISION_MISMATCH, null, null, null);

        if (activeClients.ContainsKey(username))
            return (LoginType.ALREADY_ONLINE, null, null, null);

        var account = await _accountRepository.GetByUsernameAsync(username);
        if (account == null) return (LoginType.UNUSED, null, null, null);

        if (account.IsBanned) return (LoginType.ACCOUNT_BANNED, null, null, null);
        if (password != account.Password) return (LoginType.INVALID_CREDENTIALS, null, null, null);


        var state = await _stateRepository.GetByAccountIdAsync(account.Id);
        if (state == null) return (LoginType.COULD_NOT_COMPLETE_LOGIN, null, null, null);

        var visuals = await _visualsRepository.GetByAccountIdAsync(account.Id);
        if (visuals == null) return (LoginType.COULD_NOT_COMPLETE_LOGIN, null, null, null);

        await _accountRepository.UpdateLastLoginAsync(account.Id);

        return (LoginType.ACCEPTABLE, account, state, visuals);
    }

    public async Task SaveUserDataAsync(int accountId, int x, int y, byte direction, byte movementType)
    {
        await _stateRepository.UpdatePositionAsync(accountId, x, y, direction, movementType);
        // TODO: save visuals...
    }
}