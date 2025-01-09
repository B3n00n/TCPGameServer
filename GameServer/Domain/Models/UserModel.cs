namespace GameServer.Domain.Models
{
    public class UserModel
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public int Rank { get; set; }
        public bool IsBanned { get; set; }

        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public byte Direction { get; set; }
    }
}
