using GameServer.Core.Network;
using GameServer.Handlers;

namespace GameServer.Core.Chat
{
    public interface IChatCommand
    {
        IEnumerable<string> Triggers { get; }
        int RequiredRank { get; }
        string Description { get; }
        Task ExecuteAsync(GameClient sender, string[] args, ChatPacketHandler packetHandler);
    }
}
