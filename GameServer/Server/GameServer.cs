using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Net;
using GameServer.Config;
using GameServer.Handlers;
using GameServer.Packets;

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
                await ProcessClientPackets(client);
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

        private async Task ProcessClientPackets(GameClient client)
        {
            var stream = client.GetStream();
            var opcodeBuffer = new byte[1];

            while (client.IsConnected)
            {
                try
                {
                    // Read opcode
                    int bytesRead = await stream.ReadAsync(opcodeBuffer, 0, 1);
                    if (bytesRead != 1) break;

                    byte opcode = opcodeBuffer[0];
                    var packet = new Packet(opcode, Array.Empty<byte>());

                    await _packetDispatcher.DispatchPacketAsync(client, packet);
                }
                catch (IOException) // Clean disconnection
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing packet: {ex.Message}");
                    break;
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();

            // Disconnect all clients
            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
        }
    }
}