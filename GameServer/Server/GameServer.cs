using GameServer.Config;
using GameServer.Handlers;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;

namespace GameServer
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, GameClient> _clients;
        private readonly AuthService _authService;

        #region Packet Handlers
        private readonly HandshakePacketHandler _handshakeHandler;
        private readonly LoginPacketHandler _loginHandler;
        private readonly UtilsPacketHandler _utilsHandler;
        #endregion

        private bool _isRunning;

        public GameServer()
        {
            _listener = new TcpListener(IPAddress.Any, GameConfig.PORT);
            _clients = new ConcurrentDictionary<string, GameClient>();

            var db = new DatabaseContext(GameConfig.CONNECTION_STRING);
            _authService = new AuthService(db);

            // Initialize all packet handlers
            _handshakeHandler = new HandshakePacketHandler();
            _loginHandler = new LoginPacketHandler(_authService);
            _utilsHandler = new UtilsPacketHandler();
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
            while (client.IsConnected)
            {
                try
                {
                    var opcode = await client.GetReader().ReadU8();

                    switch (opcode)
                    {
                        case 14: // Handshake
                            await _handshakeHandler.Handle(client.GetStream());
                            break;

                        case 10: // Login
                            var (success, username) = await _loginHandler.Handle(client.GetStream(), client.GetReader());
                            if (success)
                            {
                                client.SetAuthenticated(username);
                                _clients.TryAdd(username, client);
                            }
                            break;

                        case 3:  // Ping
                            await _utilsHandler.Handle(client.GetStream());
                            break;
                    }
                }
                catch (IOException)
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

        public async Task StartAsync()
        {
            _isRunning = true;
            _listener.Start();
            Console.WriteLine($"Server started on port {GameConfig.PORT}");

            while (_isRunning)
            {
                try
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(tcpClient);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener.Stop();

            foreach (var client in _clients.Values)
            {
                client.Disconnect();
            }
            _clients.Clear();
        }
    }
}