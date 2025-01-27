using GameServer.Core.Network;

namespace GameServer.Core.Events
{
    public static class PlayerEvents
    {
        public static Action<GameClient, string>? OnChatMessageSent;
    }
}