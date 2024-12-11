using System.Buffers.Binary;
using System.Text;

namespace GameServer.Packets
{
    public class PacketWriter
    {
        private readonly MemoryStream _stream;
        private readonly BinaryWriter _writer;

        public PacketWriter()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
        }

        public void WriteU8(byte value) => _writer.Write(value);

        public void WriteU16(ushort value)
        {
            var bigEndian = BinaryPrimitives.ReverseEndianness(value);
            _writer.Write(bigEndian);
        }

        public void WriteU32(uint value)
        {
            var bigEndian = BinaryPrimitives.ReverseEndianness(value);
            _writer.Write(bigEndian);
        }

        public void WriteString(string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteU32((uint)bytes.Length);
            _writer.Write(bytes);
        }

        public byte[] ToArray() => _stream.ToArray();

        public void Clear()
        {
            _stream.SetLength(0);
            _stream.Position = 0;
        }
    }
}
