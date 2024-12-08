using System.Net.Sockets;

namespace GameServer
{
    public class GameClient
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;

        public bool IsConnected => _tcpClient.Connected;
        public string Username { get; set; }
        public bool IsAuthenticated { get; set; }

        public GameClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            Username = string.Empty;
            IsAuthenticated = false;
        }

        public NetworkStream GetStream() => _stream;

        public async Task SendPacketAsync(byte[] data)
        {
            try
            {
                Console.WriteLine($"Sending {data.Length} bytes: {BitConverter.ToString(data)}");
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync(); // Make sure data is sent immediately
                Console.WriteLine("Packet sent successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public void Disconnect()
        {
            try
            {
                _tcpClient.Close();
                _stream.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client: {ex.Message}");
            }
        }
    }
}