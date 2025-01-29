using GameServer.Core.Network;

namespace GameServer.Core.Events
{
    public static class ChatEvents
    {
        public static Action<GameClient, string>? OnChatMessageSent;
    }
}