namespace GameServer.Domain.Models.Battle.Enums
{
    public enum BattleEnginePacketType : byte
    {
        Handshake = 1,
        HandshakeResponse = 2,
        Heartbeat = 3,
        HeartbeatResponse = 4,
        BattleRequest = 5,
        BattleResponse = 6,
        ServerMessage = 7,
        StatusRequest = 8,
        StatusResponse = 9,
        BattleStatusUpdate = 10,
        ClientJoinedBattle = 11,
        ClientLeftBattle = 12
    }
}
