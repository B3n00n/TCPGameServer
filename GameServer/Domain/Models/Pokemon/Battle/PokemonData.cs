using GameServer.Domain.Models.Battle;

public static class PokemonData
{
    public static Pokemon CreateCharmander()
    {
        return new Pokemon
        {
            Name = "Charmander",
            PrimaryType = PokemonType.Fire,
            MaxHP = 39,
            CurrentHP = 39,
            Attack = 52,
            Defense = 43,
            SpecialAttack = 60,
            SpecialDefense = 50,
            Speed = 65,
            Moves = new List<Move>
            {
                new Move
                {
                    Name = "Scratch",
                    Type = PokemonType.Normal,
                    Category = MoveCategory.Physical,
                    Power = 40,
                    Accuracy = 100,
                    PP = 35
                },
                new Move
                {
                    Name = "Ember",
                    Type = PokemonType.Fire,
                    Category = MoveCategory.Special,
                    Power = 40,
                    Accuracy = 100,
                    PP = 25,
                    InflictsStatus = StatusCondition.Burn
                },
                new Move
                {
                    Name = "Dragon Breath",
                    Type = PokemonType.Dragon,
                    Category = MoveCategory.Special,
                    Power = 60,
                    Accuracy = 100,
                    PP = 20
                },
                new Move
                {
                    Name = "Metal Claw",
                    Type = PokemonType.Steel,
                    Category = MoveCategory.Physical,
                    Power = 50,
                    Accuracy = 95,
                    PP = 35
                }
            }
        };
    }

    public static Pokemon CreateBulbasaur()
    {
        return new Pokemon
        {
            Name = "Bulbasaur",
            PrimaryType = PokemonType.Grass,
            SecondaryType = PokemonType.Poison,
            MaxHP = 45,
            CurrentHP = 45,
            Attack = 49,
            Defense = 49,
            SpecialAttack = 65,
            SpecialDefense = 65,
            Speed = 45,
            Moves = new List<Move>
            {
                new Move
                {
                    Name = "Tackle",
                    Type = PokemonType.Normal,
                    Category = MoveCategory.Physical,
                    Power = 40,
                    Accuracy = 100,
                    PP = 35
                },
                new Move
                {
                    Name = "Vine Whip",
                    Type = PokemonType.Grass,
                    Category = MoveCategory.Physical,
                    Power = 45,
                    Accuracy = 100,
                    PP = 25
                },
                new Move
                {
                    Name = "Poison Powder",
                    Type = PokemonType.Poison,
                    Category = MoveCategory.Status,
                    Power = 0,
                    Accuracy = 75,
                    PP = 35,
                    InflictsStatus = StatusCondition.Poison
                },
                new Move
                {
                    Name = "Leech Seed",
                    Type = PokemonType.Grass,
                    Category = MoveCategory.Status,
                    Power = 0,
                    Accuracy = 90,
                    PP = 10
                }
            }
        };
    }
}