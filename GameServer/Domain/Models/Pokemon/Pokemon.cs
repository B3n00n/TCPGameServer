namespace GameServer.Domain.Models.Battle
{
    public class Pokemon
    {
        public string Name { get; init; }
        public PokemonType PrimaryType { get; init; }
        public PokemonType? SecondaryType { get; init; }
        public StatusCondition Status { get; set; } = StatusCondition.None;

        // Stats
        public int MaxHP { get; init; }
        public int CurrentHP { get; set; }
        public int Attack { get; init; }
        public int Defense { get; init; }
        public int SpecialAttack { get; init; }
        public int SpecialDefense { get; init; }
        public int Speed { get; init; }

        public List<Move> Moves { get; init; } = new();

        public bool IsFainted => CurrentHP <= 0;

        // Helper method to get modified stats based on status conditions
        public int GetModifiedStat(Stat stat)
        {
            switch (stat)
            {
                case Stat.Attack when Status == StatusCondition.Burn:
                    return Attack / 2;
                // TODO: Handle halved speed on para...
                default:
                    return stat switch
                    {
                        Stat.HP => CurrentHP,
                        Stat.Attack => Attack,
                        Stat.Defense => Defense,
                        Stat.SpecialAttack => SpecialAttack,
                        Stat.SpecialDefense => SpecialDefense,
                        Stat.Speed => Speed,
                        _ => 0
                    };
            }
        }
    }

    public enum Stat
    {
        HP,
        Attack,
        Defense,
        SpecialAttack,
        SpecialDefense,
        Speed
    }

    public enum StatusCondition
    {
        None,
        Burn,       // 1/16 damage per turn, halves physical attack
        Poison,     // 1/8 damage per turn
        BadlyPoison // Increasing damage per turn
    }
}
