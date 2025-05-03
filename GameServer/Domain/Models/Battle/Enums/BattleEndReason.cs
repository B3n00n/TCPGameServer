namespace GameServer.Domain.Models.Battle.Enums
{
    public enum BattleEndReason
    {
        None = 0,
        PlayerVictory = 1,
        OpponentVictory = 2,
        PlayerRanAway = 3,
        PokemonCaptured = 4,
        ForcedEnd = 5,
        TurnLimitReached = 6,
        Draw = 7
    }
}
