using System.Net.Sockets;
using GameServer.Domain.Models.Player;

namespace GameServer.Core.Network
{
    public class GameClient
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private readonly PacketReader _reader;
        private readonly PacketWriter _writer;

        public PlayerData PlayerData { get; }
        public bool IsConnected => _tcpClient?.Client?.Poll(1, SelectMode.SelectRead) != true || _tcpClient?.Client?.Available != 0;

        public GameClient(TcpClient tcpClient, int index)
        {
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            _reader = new PacketReader(_stream);
            _writer = new PacketWriter();
            PlayerData = new PlayerData { Index = index };
        }

        public NetworkStream GetStream() => _stream;
        public PacketReader GetReader() => _reader;
        public PacketWriter GetWriter() => _writer;

        public void SetAuthenticated(string username)
        {
            PlayerData.Username = username;
            PlayerData.IsAuthenticated = true;
        }

        public void Disconnect()
        {
            try
            {
                PlayerData.IsAuthenticated = false;
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