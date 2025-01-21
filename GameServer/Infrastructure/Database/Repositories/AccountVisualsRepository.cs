using GameServer.Domain.Models;
using GameServer.Infrastructure.Database;

namespace GameServer.Infrastructure.Repositories
{
    public class AccountVisualsRepository
    {
        private readonly DatabaseContext _db;
        private const string TableName = "AccountVisuals";

        public AccountVisualsRepository(DatabaseContext db)
        {
            _db = db;
        }

        public async Task<AccountVisuals?> GetByAccountIdAsync(int accountId)
        {
            return await _db.QueryFirstOrDefaultAsync<AccountVisuals>(
                $"SELECT * FROM {TableName} WHERE AccountId = @AccountId",
                new { AccountId = accountId });
        }

        public async Task UpdateVisualsAsync(int accountId, int gender, int skinTone, int hairType, int hairColor,
            int hatId, int topId, int legsId)
        {
            await _db.ExecuteAsync(
                @"UPDATE AccountVisuals 
                SET Gender = @Gender, 
                    SkinTone = @SkinTone,
                    HairType = @HairType,
                    HairColor = @HairColor,
                    HatId = @HatId,
                    TopId = @TopId,
                    LegsId = @LegsId,
                    LastUpdated = CURRENT_TIMESTAMP
                WHERE AccountId = @AccountId",
                new
                {
                    AccountId = accountId,
                    Gender = gender,
                    SkinTone = skinTone,
                    HairType = hairType,
                    HairColor = hairColor,
                    HatId = hatId,
                    TopId = topId,
                    LegsId = legsId
                });
        }

        public async Task UpdateClothingAsync(int accountId, int hatId, int topId, int legsId)
        {
            await _db.ExecuteAsync(
                @"UPDATE AccountVisuals 
                SET HatId = @HatId,
                    TopId = @TopId,
                    LegsId = @LegsId,
                    LastUpdated = CURRENT_TIMESTAMP
                WHERE AccountId = @AccountId",
                new
                {
                    AccountId = accountId,
                    HatId = hatId,
                    TopId = topId,
                    LegsId = legsId
                });
        }

        public async Task UpdateAppearanceAsync(int accountId, int gender, int skinTone, int hairType, int hairColor)
        {
            await _db.ExecuteAsync(
                @"UPDATE AccountVisuals 
                SET Gender = @Gender,
                    SkinTone = @SkinTone,
                    HairType = @HairType,
                    HairColor = @HairColor,
                    LastUpdated = CURRENT_TIMESTAMP
                WHERE AccountId = @AccountId",
                new
                {
                    AccountId = accountId,
                    Gender = gender,
                    SkinTone = skinTone,
                    HairType = hairType,
                    HairColor = hairColor
                });
        }

        public async Task CreateDefaultVisualsAsync(int accountId)
        {
            await _db.ExecuteAsync(
                @"INSERT INTO AccountVisuals 
                (AccountId, Gender, SkinTone, HairType, HairColor, HatId, TopId, LegsId)
                VALUES 
                (@AccountId, 0, 0, 0, 0, 65535, 65535, 65535)",
                new { AccountId = accountId });
        }
    }
}