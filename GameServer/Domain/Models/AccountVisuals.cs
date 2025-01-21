namespace GameServer.Domain.Models
{
    public class AccountVisuals
    {
        public int AccountId { get; set; }
        public int Gender { get; set; }
        public int SkinTone { get; set; }
        public int HairType { get; set; }
        public int HairColor { get; set; }
        public int HatId { get; set; }
        public int TopId { get; set; }
        public int LegsId { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}