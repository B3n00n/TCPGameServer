using GameServer.Packets;
using System.Diagnostics;

namespace GameServer.Handlers
{
    public class UtilsPacketHandler : IPacketHandler
    {
        private static readonly byte[] HandledOpcodes = { 3 };  // Ping opcode
        private readonly Stopwatch _stopwatch;
        private const int MAX_PING = 10000;

        public UtilsPacketHandler()
        {
            _stopwatch = new Stopwatch();
        }

        public IEnumerable<byte> GetHandledOpcodes() => HandledOpcodes;

        public async Task HandlePacketAsync(GameClient client, Packet packet)
        {
            if (packet.Opcode == 3)
            {
                await HandlePingAsync(client);
            }
        }

        private async Task HandlePingAsync(GameClient client)
        {
            _stopwatch.Restart();

            try
            {
                var buffer = new StreamBuffer();
                buffer.WriteU8(3);
                await client.SendPacketAsync(buffer.ToArray());

                _stopwatch.Stop();
                var pingTime = _stopwatch.ElapsedMilliseconds;

                if (pingTime > MAX_PING)
                {
                    Console.WriteLine($"High latency detected for {client.Username}: {pingTime}ms");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling ping: {ex.Message}");
            }
        }
    }
}