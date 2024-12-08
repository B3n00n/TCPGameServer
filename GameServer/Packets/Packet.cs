namespace GameServer.Packets
{
    public class Packet
    {
        public byte Opcode { get; }
        public byte[] Payload { get; }

        public Packet(byte opcode, byte[] payload)
        {
            Opcode = opcode;
            Payload = payload;
        }
    }
}