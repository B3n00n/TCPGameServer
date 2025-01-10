namespace GameServer.Domain.Models.Player
{
    public class PlayerData
    {
        public int AccountId { get; set; } = 0;
        public string Username { get; set; } = string.Empty;
        public int Index { get; set; } = 0;
        public Position Position { get; set; } = new(0, 0);
        public byte Direction { get; set; } = 0;
        public byte MovementType { get; set; } = 0;
        public bool IsAuthenticated { get; set; } = false;
        public byte Rank { get; set; } = 0;
    }
}