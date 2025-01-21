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
        public bool IsMuted { get; set; } = false;
        public byte Rank { get; set; } = 0;


        // Visual data
        public byte Gender { get; set; } = 0;
        public byte SkinTone { get; set; } = 0;
        public byte HairType { get; set; } = 0;
        public byte HairColor { get; set; } = 0;
        public ushort HatId { get; set; } = 65535;
        public ushort TopId { get; set; } = 65535;
        public ushort LegsId { get; set; } = 65535;
    }
}