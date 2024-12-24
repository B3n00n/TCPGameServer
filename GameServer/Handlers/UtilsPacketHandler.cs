using GameServer.Core.Network;
using System.Diagnostics;
using System.Net.Sockets;

namespace GameServer.Handlers
{
    public class UtilsPacketHandler
    {
        private readonly Stopwatch _stopwatch;
        private const int MAX_PING = 10000;

        public UtilsPacketHandler()
        {
            _stopwatch = new Stopwatch();
        }

        public async Task HandlePing(NetworkStream stream)
        {
            _stopwatch.Restart();

            var response = new PacketWriter();
            response.WriteU8(3);
            response.WriteU16(0);
            await stream.WriteAsync(response.ToArray());

            _stopwatch.Stop();
            var pingTime = _stopwatch.ElapsedMilliseconds;

            if (pingTime > MAX_PING)
            {
                Console.WriteLine($"High latency detected: {pingTime}ms");
            }
        }
    }
}