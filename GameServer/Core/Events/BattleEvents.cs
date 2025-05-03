namespace GameServer.Core.Events
{
    public static class BattleEvents
    {
        public static event Action<string, byte> OnBattleEnded;
        public static event Action<string, string> OnPlayerJoinedBattle;
        public static event Action<string, string> OnPlayerLeftBattle;
    }
}
