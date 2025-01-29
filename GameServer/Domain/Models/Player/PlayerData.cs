namespace GameServer.Domain.Models.Player
{
    public class PlayerData
    {
        // Reference to database models
        public Account? Account { get; private set; }
        public AccountState? State { get; private set; }
        public AccountVisuals? Visuals { get; private set; }

        // Runtime-only state
        public int Index { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public PlayerData(int index)
        {
            Index = index;
            IsAuthenticated = false;
        }

        public void SetData(Account account, AccountState state, AccountVisuals visuals)
        {
            Account = account;
            State = state;
            Visuals = visuals;

            IsAuthenticated = true;
        }

        // Convenience properties that map to the underlying models
        public int AccountId => Account?.Id ?? 0;
        public string Username => Account?.Username ?? string.Empty;
        public bool IsMuted => Account?.IsMuted ?? false;
        public byte Rank => (byte)(Account?.Rank ?? 0);

        public Position Position => new(State?.PositionX ?? 0, State?.PositionY ?? 0);
        public byte Direction => State?.Direction ?? 0;
        public byte MovementType => State?.MovementType ?? 0;

        public byte Gender => (byte)(Visuals?.Gender ?? 0);
        public byte SkinTone => (byte)(Visuals?.SkinTone ?? 0);
        public byte HairType => (byte)(Visuals?.HairType ?? 0);
        public byte HairColor => (byte)(Visuals?.HairColor ?? 0);
        public ushort HatId => (ushort)(Visuals?.HatId ?? 65535);
        public ushort TopId => (ushort)(Visuals?.TopId ?? 65535);
        public ushort LegsId => (ushort)(Visuals?.LegsId ?? 65535);
    }
}