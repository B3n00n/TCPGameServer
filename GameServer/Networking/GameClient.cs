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
                await _stream.WriteAsync(data, 0, data.Length);
                await _stream.FlushAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending packet: {ex.Message}");
                throw;
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