using GameServer.Domain.Models;
using GameServer.Infrastructure.Database;


namespace GameServer.Infrastructure.Repositories
{
    public class AccountStateRepository
    {
        private readonly DatabaseContext _db;
        private const string TableName = "AccountStates";

        public AccountStateRepository(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<AccountState?> GetByAccountIdAsync(int accountId)
        {
            return await _db.QueryFirstOrDefaultAsync<AccountState>(
                $"SELECT * FROM {TableName} WHERE AccountId = @AccountId",
                new { AccountId = accountId });
        }

        public async Task UpdatePositionAsync(int accountId, int x, int y, byte direction, byte movementType)
        {
            await _db.ExecuteAsync(
                @"UPDATE AccountStates 
                SET PositionX = @X, PositionY = @Y, 
                    Direction = @Direction, MovementType = @MovementType,
                    LastUpdated = CURRENT_TIMESTAMP
                WHERE AccountId = @AccountId",
                new { AccountId = accountId, X = x, Y = y, Direction = direction, MovementType = movementType });
        }
    }
}