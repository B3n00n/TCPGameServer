namespace GameServer.Packets
{
    public class Packet
    {
        public byte Opcode { get; }
        public ushort Length { get; private set; }
        public byte[] Payload { get; }

        public Packet(byte opcode, byte[] payload)
        {
            Opcode = opcode;
            Length = (ushort)payload.Length;
            Payload = payload;
        }
    }
}