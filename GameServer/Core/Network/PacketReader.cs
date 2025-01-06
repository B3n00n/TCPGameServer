using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace GameServer.Core.Network
{
    public class PacketReader
    {
        private readonly NetworkStream _networkStream;

        public PacketReader(NetworkStream networkStream)
        {
            _networkStream = networkStream;
        }

        public async Task<byte> ReadU8()
        {
            var buffer = new byte[1];
            await _networkStream.ReadAsync(buffer);
            return buffer[0];
        }

        public async Task<ushort> ReadU16()
        {
            var buffer = new byte[2];
            await _networkStream.ReadAsync(buffer);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public async Task<uint> ReadU32()
        {
            var buffer = new byte[4];
            await _networkStream.ReadAsync(buffer);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public async Task<string> ReadString()
        {
            var length = (int)await ReadU32();

            var buffer = new byte[length];
            await _networkStream.ReadAsync(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public async Task<string> ReadAsciiString()
        {
            var length = await ReadU16();

            var buffer = new byte[length];
            await _networkStream.ReadAsync(buffer, 0, length);
            return Encoding.ASCII.GetString(buffer);
        }
    }
}