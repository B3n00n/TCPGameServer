using GameServer.Packets;
using System.Net.Sockets;

namespace GameServer
{
    public class GameClient
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly PacketReader _reader;
        private readonly PacketWriter _writer;

        public bool IsConnected => _tcpClient.Connected;
        public string Username { get; private set; }
        public bool IsAuthenticated { get; private set; }

        public GameClient(TcpClient tcpClient)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            _reader = new PacketReader(_stream);
            _writer = new PacketWriter();
            Username = string.Empty;
            IsAuthenticated = false;
        }

        public NetworkStream GetStream() => _stream;
        public PacketReader GetReader() => _reader;
        public PacketWriter GetWriter() => _writer;

        public void SetAuthenticated(string username)
        {
            Username = username;
            IsAuthenticated = true;
        }

        public void Disconnect()
        {
            try
            {
                _tcpClient.Close();
                _stream.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error disconnecting client: {ex.Message}");
            }
        }
    }
}