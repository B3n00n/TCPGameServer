using GameServer.Packets;
using System.Net.Sockets;


namespace GameServer.Handlers
{
    public class HandshakePacketHandler : PacketHandlerBase
    {
        public async Task Handle(NetworkStream stream)
        {
            var response = CreateResponsePacket();
            response.WriteU8(0);
            await SendPacketAsync(stream, response.ToArray());
        }
    }
}