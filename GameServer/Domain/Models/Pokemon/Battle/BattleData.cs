namespace GameServer.Domain.Models.Battle
{
    public static class BattleData
    {
        private static readonly Random _random = new();

        public static bool RollAccuracy(int accuracy) => _random.Next(1, 101) <= accuracy;

        public static bool RollCriticalHit() => _random.Next(1, 17) == 1;

        public static bool RollStatusEffect(int chance = 10) => _random.Next(100) < chance;
    }
}