using GameServer.Domain.Models;
using GameServer.Infrastructure.Database;

namespace GameServer.Infrastructure.Repositories
{
    public class AccountRepository
    {
        private readonly DatabaseContext _db;
        private const string TableName = "Accounts";

        public AccountRepository(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<Account?> GetByUsernameAsync(string username)
        {
            return await _db.QueryFirstOrDefaultAsync<Account>(
                $"SELECT * FROM {TableName} WHERE Username = @Username COLLATE NOCASE",
                new { Username = username });
        }

        public async Task UpdateLastLoginAsync(int accountId)
        {
            await _db.ExecuteAsync(
                $"UPDATE {TableName} SET LastLoginAt = CURRENT_TIMESTAMP WHERE Id = @Id",
                new { Id = accountId });
        }
    }
}