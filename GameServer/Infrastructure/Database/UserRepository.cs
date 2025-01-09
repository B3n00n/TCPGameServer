using GameServer.Domain.Models;

namespace GameServer.Infrastructure.Database
{
    public class UserRepository
    {
        private readonly DatabaseContext _db;
        private const string TableName = "Users";

        public UserRepository(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<UserModel?> GetByUsernameAsync(string username)
            => await _db.QueryFirstOrDefaultAsync<UserModel>(
                $"SELECT * FROM {TableName} WHERE Username = @Username",
                new { Username = username });

        public async Task UpdatePositionAsync(string username, int x, int y, int direction, int movementType)
            => await _db.ExecuteAsync(
                $"UPDATE {TableName} SET PositionX = @X, PositionY = @Y, Direction = @Direction, MovementType = @MovementType WHERE Username = @Username",
                new { Username = username, X = x, Y = y, Direction = direction, MovementType = movementType });
    }
}
