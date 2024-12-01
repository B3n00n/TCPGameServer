using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packets
{
    public interface IPacketHandler
    {
        // Get all opcodes this handler is responsible for
        IEnumerable<byte> GetHandledOpcodes();

        // Handle a specific packet
        Task HandlePacketAsync(GameClient client, Packet packet);
    }
}
