using System.Text;

namespace GameServer.Packets
{
    public class BitBuffer
    {
        private byte[] buffer;
        private int bitPosition;
        private int writePosition;
        private readonly int[] BitMasks;

        public BitBuffer(int initialCapacity = 256)
        {
            buffer = new byte[initialCapacity];
            bitPosition = 0;
            writePosition = 0;

            BitMasks = new int[32];
            for (int i = 0; i < 32; i++)
            {
                BitMasks[i] = (1 << i) - 1;
            }
        }

        public void WriteBits(int numBits, int value)
        {
            value &= BitMasks[numBits];
            int bytePos = bitPosition >> 3;
            int bitOffset = bitPosition & 7;
            bitPosition += numBits;

            if ((bitPosition + 7) >> 3 > buffer.Length)
            {
                Array.Resize(ref buffer, buffer.Length * 2);
            }

            writePosition = Math.Max(writePosition, (bitPosition + 7) >> 3);

            while (numBits > 0)
            {
                int bitsThisByte = Math.Min(8 - bitOffset, numBits);
                int mask = BitMasks[bitsThisByte] << (8 - bitOffset - bitsThisByte);
                int alignedValue = (value >> (numBits - bitsThisByte)) & BitMasks[bitsThisByte];

                buffer[bytePos] &= (byte)~mask;
                buffer[bytePos] |= (byte)(alignedValue << (8 - bitOffset - bitsThisByte));

                numBits -= bitsThisByte;
                bitOffset += bitsThisByte;

                if (bitOffset >= 8)
                {
                    bytePos++;
                    bitOffset = 0;
                }
            }
        }

        public void WriteString(string value)
        {
            ReadOnlySpan<byte> stringBytes = Encoding.UTF8.GetBytes(value);
            WriteBits(8, Math.Min(stringBytes.Length, 255));
            foreach (byte b in stringBytes)
            {
                WriteBits(8, b);
            }
        }

        public byte[] ToArray()
        {
            byte[] result = new byte[writePosition];
            Buffer.BlockCopy(buffer, 0, result, 0, writePosition);
            return result;
        }

        public void Reset()
        {
            bitPosition = 0;
            writePosition = 0;
        }
    }
}