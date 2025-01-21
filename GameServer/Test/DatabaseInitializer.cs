using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Database;
using System.IO;

namespace GameServer.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly DatabaseContext _db;

        public DatabaseInitializer(DatabaseContext db)
        {
            _db = db;
        }

        public async Task InitializeAsync()
        {
            EnsureDatabaseExists();
            await CreateTablesAsync();
            await SeedInitialDataAsync();
        }

        private void EnsureDatabaseExists()
        {
            var dbPath = Path.GetDirectoryName(GameConfig.CONNECTION_STRING.Replace("Data Source=", ""));
            if (!string.IsNullOrEmpty(dbPath) && !Directory.Exists(dbPath))
            {
                Directory.CreateDirectory(dbPath);
            }
        }

        private async Task CreateTablesAsync()
        {
            Console.WriteLine("Initializing database tables...");

            // Create Accounts table
            await _db.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS Accounts (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            Password TEXT NOT NULL,
            Rank INTEGER NOT NULL DEFAULT 0,
            IsBanned BOOLEAN NOT NULL DEFAULT 0,
            IsMuted BOOLEAN NOT NULL DEFAULT 0,
            CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            LastLoginAt DATETIME,
            UNIQUE(Username COLLATE NOCASE)
        )");

            // Create AccountStates table
            await _db.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS AccountStates (
            AccountId INTEGER PRIMARY KEY,
            PositionX INTEGER NOT NULL DEFAULT 0,
            PositionY INTEGER NOT NULL DEFAULT 0,
            Direction INTEGER NOT NULL DEFAULT 0,
            MovementType INTEGER NOT NULL DEFAULT 0,
            LastUpdated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
        )");

            // Create AccountVisuals table
            await _db.ExecuteAsync(@"
        CREATE TABLE IF NOT EXISTS AccountVisuals (
            AccountId INTEGER PRIMARY KEY,
            Gender INTEGER NOT NULL DEFAULT 0,
            SkinTone INTEGER NOT NULL DEFAULT 0,
            HairType INTEGER NOT NULL DEFAULT 0,
            HairColor INTEGER NOT NULL DEFAULT 0,
            HatId INTEGER NOT NULL DEFAULT 65535,
            TopId INTEGER NOT NULL DEFAULT 65535,
            LegsId INTEGER NOT NULL DEFAULT 65535,
            LastUpdated DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
            FOREIGN KEY (AccountId) REFERENCES Accounts(Id)
        )");

            // Create indexes for AccountVisuals
            await _db.ExecuteAsync(@"
        CREATE INDEX IF NOT EXISTS idx_account_visuals_hatid ON AccountVisuals(HatId);
        CREATE INDEX IF NOT EXISTS idx_account_visuals_topid ON AccountVisuals(TopId);
        CREATE INDEX IF NOT EXISTS idx_account_visuals_legsid ON AccountVisuals(LegsId);
    ");

            Console.WriteLine("Database tables initialized successfully.");
        }

        private async Task SeedInitialDataAsync()
        {
            // Check if we already have any accounts
            var hasAccounts = await _db.QueryFirstOrDefaultAsync<int>("SELECT COUNT(*) FROM Accounts") > 0;
            if (hasAccounts)
            {
                Console.WriteLine("Database already contains data, skipping initialization.");
                return;
            }

            Console.WriteLine("Seeding initial data...");

            // Insert test accounts
            await _db.ExecuteAsync(@"
        INSERT INTO Accounts (Username, Password, Rank, IsBanned, CreatedAt) 
        VALUES 
            ('a', 'a', 6, 0, CURRENT_TIMESTAMP),
            ('b', 'b', 4, 0, CURRENT_TIMESTAMP),
            ('c', 'c', 2, 0, CURRENT_TIMESTAMP),
            ('d', 'd', 0, 0, CURRENT_TIMESTAMP)");

            // Insert their states
            await _db.ExecuteAsync(@"
        INSERT INTO AccountStates (AccountId, PositionX, PositionY, Direction, MovementType)
        VALUES 
            (1, 0, 0, 0, 0),
            (2, 0, 0, 1, 0),
            (3, 0, 0, 2, 0),
            (4, 0, 0, 3, 0)");

            // Insert their visual data
            await _db.ExecuteAsync(@"
        INSERT INTO AccountVisuals (AccountId, Gender, SkinTone, HairType, HairColor, HatId, TopId, LegsId)
        VALUES 
            (1, 0, 0, 0, 0, 65535, 65535, 65535),
            (2, 1, 1, 1, 1, 65535, 65535, 65535),
            (3, 0, 2, 2, 2, 65535, 65535, 65535),
            (4, 1, 0, 3, 3, 65535, 65535, 65535)");

            Console.WriteLine("Initial data seeded successfully.");
        }
    }
}