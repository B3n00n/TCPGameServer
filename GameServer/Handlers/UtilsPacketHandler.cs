using GameServer.Packets;
using System.Diagnostics;
using System.Net.Sockets;

namespace GameServer.Handlers
{
    public class UtilsPacketHandler : PacketHandlerBase
    {
        private readonly Stopwatch _stopwatch;
        private const int MAX_PING = 10000;

        public UtilsPacketHandler()
        {
            _stopwatch = new Stopwatch();
        }

        public async Task Handle(NetworkStream stream)
        {
            _stopwatch.Restart();

            var response = CreateResponsePacket();
            response.WriteU8(3);
            response.WriteU16(0);
            await SendPacketAsync(stream, response.ToArray());

            _stopwatch.Stop();
            var pingTime = _stopwatch.ElapsedMilliseconds;

            if (pingTime > MAX_PING)
            {
                Console.WriteLine($"High latency detected: {pingTime}ms");
            }
        }
    }
}