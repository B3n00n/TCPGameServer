using GameServer.Packets;
using System.Net.Sockets;

public abstract class PacketHandlerBase
{
    protected async Task SendPacketAsync(NetworkStream stream, byte[] data)
    {
        try
        {
            await stream.WriteAsync(data);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending packet: {ex.Message}");
            throw;
        }
    }

    protected PacketWriter CreateResponsePacket()
    {
        return new PacketWriter();
    }
}