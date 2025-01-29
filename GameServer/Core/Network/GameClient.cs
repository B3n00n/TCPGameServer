using GameServer.Core.Network;
using GameServer.Domain.Models.Player;
using GameServer.Domain.Models;
using System.Net.Sockets;

public class GameClient
{
    private readonly TcpClient _tcpClient;
    private readonly NetworkStream _stream;
    private readonly PacketReader _reader;
    private readonly PacketWriter _writer;

    public PlayerData Data { get; }
    public bool IsConnected => _tcpClient?.Client?.Poll(1, SelectMode.SelectRead) != true || _tcpClient?.Client?.Available != 0;

    public GameClient(TcpClient tcpClient, int index)
    {
        _tcpClient = tcpClient;
        _stream = tcpClient.GetStream();
        _reader = new PacketReader(_stream);
        _writer = new PacketWriter();
        Data = new PlayerData(index);
    }

    public NetworkStream GetStream() => _stream;
    public PacketReader GetReader() => _reader;
    public PacketWriter GetWriter() => _writer;

    public void SetData(Account account, AccountState state, AccountVisuals visuals) => Data.SetData(account, state, visuals);

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