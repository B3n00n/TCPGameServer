using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packets
{
    public class PacketDispatcher
    {
        private readonly Dictionary<byte, IPacketHandler> _opcodeHandlers = new();

        public PacketDispatcher(IEnumerable<IPacketHandler> handlers)
        {
            // Register all handlers and their opcodes
            foreach (var handler in handlers)
            {
                foreach (var opcode in handler.GetHandledOpcodes())
                {
                    _opcodeHandlers[opcode] = handler;
                }
            }
        }

        public async Task DispatchPacketAsync(GameClient client, Packet packet)
        {
            if (_opcodeHandlers.TryGetValue(packet.Opcode, out var handler))
            {
                await handler.HandlePacketAsync(client, packet);
            }
            else
            {
                Console.WriteLine($"No handler registered for opcode: {packet.Opcode}");
            }
        }
    }
}
