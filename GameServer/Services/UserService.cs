using GameServer.Core.Network;
using GameServer.Domain.Models;
using GameServer.Handlers;
using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Database;
using System.Collections.Concurrent;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(DatabaseContext db)
    {
        _userRepository = new UserRepository(db);
    }

    public async Task<(LoginType Status, UserModel? User)> AuthenticateAsync(
        string username,
        string password,
        uint revision,
        ConcurrentDictionary<string, GameClient> activeClients)
    {
        if (revision != GameConfig.REVISION)
            return (LoginType.REVISION_MISMATCH, null);

        if (activeClients.ContainsKey(username))
            return (LoginType.ALREADY_ONLINE, null);

        var user = await _userRepository.GetByUsernameAsync(username);

        if (user == null)
            return (LoginType.UNUSED, null);

        if (user.IsBanned)
            return (LoginType.ACCOUNT_BANNED, null);

        if (password != user.Password)
            return (LoginType.INVALID_CREDENTIALS, null);

        return (LoginType.ACCEPTABLE, user);
    }

    public async Task SaveUserDataAsync(string username, int x, int y, int direction, int movementType)
    {
        await _userRepository.UpdatePositionAsync(username, x, y, direction, movementType);
    }
}