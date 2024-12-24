using System.Collections.Concurrent;
using GameServer.Handlers;
using GameServer.Domain.Models;
using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Database;
using GameServer.Core.Network;

public class UserService
{
    private readonly DatabaseContext _db;

    public UserService(DatabaseContext db)
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

    public async Task SaveUserPositionAsync(string username, int x, int y)
    {
        try
        {
            await _db.ExecuteAsync(
                "UPDATE Users SET PositionX = @X, PositionY = @Y WHERE Username = @Username",
                new { X = x, Y = y, Username = username });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving position for {username}: {ex.Message}");
            throw; // Rethrow to handle in the game server
        }
    }
}