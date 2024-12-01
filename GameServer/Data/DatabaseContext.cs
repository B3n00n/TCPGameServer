using Microsoft.Data.Sqlite;
using Dapper;

namespace GameServer
{
    public class DatabaseContext
    {
        private readonly string _connectionString;

        public DatabaseContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object param)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            return await connection.QueryFirstOrDefaultAsync<T>(sql, param);
        }
    }
}