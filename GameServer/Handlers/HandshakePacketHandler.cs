using GameServer.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Handlers
{
    public class HandshakePacketHandler : IPacketHandler
    {
        private static readonly byte[] HandledOpcodes = { 14 };

        public IEnumerable<byte> GetHandledOpcodes() => HandledOpcodes;

        public async Task HandlePacketAsync(GameClient client, Packet packet)
        {
            if (packet.Opcode == 14)
            {
                // Just send a single byte 0 as response
                var buffer = new StreamBuffer();
                buffer.WriteU8(0);
                await client.SendPacketAsync(buffer.ToArray());
                Console.WriteLine("Sent handshake response");
            }
        }
    }
}