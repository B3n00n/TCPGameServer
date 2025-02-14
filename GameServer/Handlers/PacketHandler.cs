using GameServer.Core.Network;

namespace GameServer.Handlers
{
    public abstract class PacketHandler
    {
        protected readonly BitBuffer headerBuffer;
        protected readonly BitBuffer payloadBuffer;

        protected PacketHandler()
        {
            headerBuffer = new BitBuffer(3);    // Fixed size for opcode + length
            payloadBuffer = new BitBuffer(256); // Typical payload size
        }

        protected byte[] CreatePacket(byte opcode, Action<BitBuffer> writePayload)
        {
            payloadBuffer.Reset();
            writePayload(payloadBuffer);
            var payload = payloadBuffer.ToArray();

            headerBuffer.Reset();
            headerBuffer.WriteBits(8, opcode);
            headerBuffer.WriteBits(16, payload.Length);
            var header = headerBuffer.ToArray();

            var finalPacket = new byte[header.Length + payload.Length];
            Buffer.BlockCopy(header, 0, finalPacket, 0, header.Length);
            Buffer.BlockCopy(payload, 0, finalPacket, header.Length, payload.Length);

            return finalPacket;
        }
    }
}