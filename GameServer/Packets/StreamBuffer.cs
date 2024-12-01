using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Packets
{
    public class StreamBuffer
    {
        private MemoryStream _stream;
        private BinaryWriter _writer;
        private BinaryReader _reader;

        public StreamBuffer()
        {
            _stream = new MemoryStream();
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);
        }

        public StreamBuffer(byte[] data)
        {
            _stream = new MemoryStream(data);
            _writer = new BinaryWriter(_stream);
            _reader = new BinaryReader(_stream);
        }

        // Write methods
        public void WriteU8(byte value) => _writer.Write(value);
        public void WriteU16(ushort value) => _writer.Write(BinaryPrimitives.ReverseEndianness(value));
        public void WriteU32(uint value) => _writer.Write(BinaryPrimitives.ReverseEndianness(value));
        public void WriteString(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            WriteU32((uint)bytes.Length);
            _writer.Write(bytes);
        }

        // Read methods
        public byte ReadU8() => _reader.ReadByte();
        public ushort ReadU16() => BinaryPrimitives.ReverseEndianness(_reader.ReadUInt16());
        public uint ReadU32() => BinaryPrimitives.ReverseEndianness(_reader.ReadUInt32());
        public string ReadString()
        {
            var length = (int)ReadU32();
            if (length <= 0) return "";
            var bytes = _reader.ReadBytes(length);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public byte[] ToArray()
        {
            var position = _stream.Position;
            var data = _stream.ToArray();
            _stream.Position = position;
            return data;
        }
    }
}
