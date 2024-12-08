using GameServer.Config;
using GameServer.Handlers;
using GameServer.Packets;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GameServer
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly PacketDispatcher _packetDispatcher;
        private readonly ConcurrentDictionary<string, GameClient> _clients;
        private readonly AuthService _authService;
        private bool _isRunning;

        public GameServer()
        {
            _listener = new TcpListener(IPAddress.Any, GameConfig.PORT);
            _clients = new ConcurrentDictionary<string, GameClient>();

            var db = new DatabaseContext(GameConfig.CONNECTION_STRING);
            _authService = new AuthService(db);

            var handlers = new IPacketHandler[]
            {
                new HandshakePacketHandler(),
                new LoginPacketHandler(_authService),
                new UtilsPacketHandler(),
            };

            _packetDispatcher = new PacketDispatcher(handlers);
        }

        public async Task StartAsync()
        {
            _isRunning = true;
            _listener.Start();
            Console.WriteLine($"Server started on port {GameConfig.PORT}");

            while (_isRunning)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            Console.WriteLine($"New client connected from {tcpClient.Client.RemoteEndPoint}");
            var client = new GameClient(tcpClient);

            try
            {
                while (client.IsConnected)
                {
                    // Just read the opcode byte
                    var opcodeByte = new byte[1];
                    var bytesRead = await client.GetStream().ReadAsync(opcodeByte, 0, 1);
                    if (bytesRead != 1) break;

                    var packet = new Packet(opcodeByte[0], Array.Empty<byte>());
                    await _packetDispatcher.DispatchPacketAsync(client, packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
            }
            finally
            {
                if (!string.IsNullOrEmpty(client.Username))
                {
                    _authService.RemoveOnlinePlayer(client.Username);
                    _clients.TryRemove(client.Username, out _);
                }
                client.Disconnect();
            }
        }
    }
}