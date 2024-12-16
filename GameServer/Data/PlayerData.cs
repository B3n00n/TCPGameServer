namespace GameServer
{
    public class PlayerData
    {
        public string Username { get; set; }
        public int Index { get; private set; }
        public Position Position { get; set; }
        public byte Direction { get; set; }
        public byte MovementType { get; set; }
        public bool IsAuthenticated { get; set; }

        private static int _nextIndex = 1;

        public PlayerData()
        {
            Username = string.Empty;
            Index = _nextIndex++;
            Position = new Position(0, 0);
            Direction = 0;
            MovementType = 0;
            IsAuthenticated = false;
        }
    }
}