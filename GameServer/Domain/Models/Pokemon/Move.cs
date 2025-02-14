namespace GameServer.Domain.Models.Battle
{
    public class Move
    {
        public string Name { get; init; }
        public PokemonType Type { get; init; }
        public MoveCategory Category { get; init; }
        public int Power { get; init; }
        public int Accuracy { get; init; }
        public int Priority { get; init; } = 0;
        public int PP { get; init; }
        public StatusCondition? InflictsStatus { get; init; }

        // For a PoC we'll keep it simple, but could add:
        // - Stat boost/reduction chances
        // - Secondary effects
        // - Critical hit ratio modifications
        // etc.
    }

    public enum MoveCategory
    {
        Physical,
        Special,
        Status
    }
}
