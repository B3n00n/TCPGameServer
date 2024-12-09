using System.Buffers.Binary;
using System.Net.Sockets;
using System.Text;

namespace GameServer.Packets
{
    public class StreamBuffer
    {
        private readonly MemoryStream _stream;
        private readonly BinaryReader _reader;
        private readonly BinaryWriter _writer;
        private readonly NetworkStream _networkStream;

        public StreamBuffer()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);
        }

        public StreamBuffer(NetworkStream networkStream)
        {
            _networkStream = networkStream;
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);
        }

        // Write methods
        public void WriteU8(byte value) => _writer.Write(value);
        public void WriteU16(ushort value) => _writer.Write(BinaryPrimitives.ReverseEndianness(value));
        public void WriteU32(uint value) => _writer.Write(BinaryPrimitives.ReverseEndianness(value));
        public void WriteString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteU32((uint)bytes.Length);
            _writer.Write(bytes);
        }

        public void WriteBytes(byte[] bytes) => _writer.Write(bytes, 0, bytes.Length);

        // Read methods from NetworkStream
        public async Task<byte> ReadU8()
        {
            var buffer = new byte[1];
            await _networkStream.ReadAsync(buffer, 0, 1);
            return buffer[0];
        }

        public async Task<ushort> ReadU16()
        {
            var buffer = new byte[2];
            await _networkStream.ReadAsync(buffer, 0, 2);
            return BinaryPrimitives.ReadUInt16BigEndian(buffer);
        }

        public async Task<uint> ReadU32()
        {
            var buffer = new byte[4];
            await _networkStream.ReadAsync(buffer, 0, 4);
            return BinaryPrimitives.ReadUInt32BigEndian(buffer);
        }

        public async Task<string> ReadString()
        {
            var length = (int)await ReadU32();
            if (length <= 0) return string.Empty;

            var buffer = new byte[length];
            await _networkStream.ReadAsync(buffer, 0, length);
            return Encoding.UTF8.GetString(buffer);
        }

        public byte[] ToArray() => _stream.ToArray();
    }
}