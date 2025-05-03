namespace GameServer.Infrastructure.Config
{
    public static class GameConfig
    {
        public const int PORT = 30811;
        public const int REVISION = 1;
        public const int MAX_PLAYERS = 5000;

        public const string CONNECTION_STRING = "Data Source=gamedb.sqlite";
    }
}