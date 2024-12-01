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
            Console.WriteLine($"Handling packet with opcode: {packet.Opcode}");

            if (packet.Opcode == 14)
            {
                var buffer = new StreamBuffer();
                buffer.WriteU8(0);  // Confirmation byte
                buffer.WriteU16(0); // Add length for protocol consistency

                var response = buffer.ToArray();
                Console.WriteLine($"Sending handshake response, length: {response.Length}");

                await client.SendPacketAsync(response);
                Console.WriteLine("Sent handshake response");
            }
        }
    }
}
