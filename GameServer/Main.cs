using System.Threading.Tasks;
using GameServer.Config;
using Microsoft.Data.Sqlite;

namespace GameServer
{
    class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Starting game server...");

        try
        {
            var server = new GameServer();
            await server.StartAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Critical error: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Server shutdown complete");
        }
    }
}
}