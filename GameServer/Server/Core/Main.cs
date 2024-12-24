namespace GameServer.Server.Core
{
    class Program
    {
        static async Task Main()
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