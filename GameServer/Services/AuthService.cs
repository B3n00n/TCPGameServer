using GameServer.Data;
using GameServer;
using System.Collections.Concurrent;
using GameServer.Config;

public class AuthService
{
    private readonly DatabaseContext _db;

    public AuthService(DatabaseContext db)
    {
        _db = db;
    }

    public async Task<(LoginType Status, User? User)> AuthenticateAsync(string username, string password, uint revision, ConcurrentDictionary<string, GameClient> activeClients)
    {
        if (revision != GameConfig.REVISION)
            return (LoginType.REVISION_MISMATCH, null);

        var user = await _db.QueryFirstOrDefaultAsync<User>(
            "SELECT * FROM Users WHERE Username = @Username",
            new { Username = username });

        if (user == null)
            return (LoginType.UNUSED, null);

        if (user.IsBanned)
            return (LoginType.ACCOUNT_BANNED, null);

        if (activeClients.ContainsKey(username))
            return (LoginType.ALREADY_ONLINE, null);

        if (password != user.Password)
            return (LoginType.INVALID_CREDENTIALS, null);

        return (LoginType.ACCEPTABLE, user);
    }
}