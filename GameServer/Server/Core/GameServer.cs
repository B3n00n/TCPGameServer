using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using GameServer.Handlers;
using GameServer.Core;
using GameServer.Domain.Models.Player;
using GameServer.Infrastructure.Config;
using GameServer.Infrastructure.Database;
using GameServer.Core.Network;
using GameServer.Core.Chat;

namespace GameServer.Server.Core
{
    public class GameServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<string, GameClient> _clients;
        private readonly UserService _userService;
        private readonly ChatService _chatService;
        private readonly Pool<PlayerData> _playerIndexPool;

        #region Packet Handlers
        private readonly HandshakePacketHandler _handshakeHandler;
        private readonly LoginPacketHandler _loginHandler;
        private readonly UtilsPacketHandler _utilsHandler;
        private readonly PlayerPacketHandler _playerHandler;
        #endregion

        private bool _isRunning;

        public GameServer()
        {
            _listener = new TcpListener(IPAddress.Any, GameConfig.PORT);
            _clients = new ConcurrentDictionary<string, GameClient>();
            _playerIndexPool = new Pool<PlayerData>(5000);

            var db = new DatabaseContext(GameConfig.CONNECTION_STRING);
            _userService = new UserService(db);
            _chatService = new ChatService(_clients);

            // Initialize all packet handlers
            _handshakeHandler = new HandshakePacketHandler();
            _loginHandler = new LoginPacketHandler(_userService);
            _utilsHandler = new UtilsPacketHandler();
            _playerHandler = new PlayerPacketHandler();
        }

        private async Task HandleClientAsync(TcpClient tcpClient)
        {
            Console.WriteLine($"New client connected from {tcpClient.Client.RemoteEndPoint}");
            var client = new GameClient(tcpClient, _playerIndexPool.Get());

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
                if (!string.IsNullOrEmpty(client.PlayerData.Username))
                {
                    await _userService.SaveUserDataAsync(client.PlayerData.Username, client.PlayerData.Position.X, client.PlayerData.Position.Y, client.PlayerData.Direction);
                    await _playerHandler.HandleLogout(client, _clients);
                    _clients.TryRemove(client.PlayerData.Username, out _);
                }

                _playerIndexPool.Return(client.PlayerData.Index);
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
                        case 2: // Player
                            if (client.PlayerData.IsAuthenticated)
                                await _playerHandler.HandleMovement(client, _clients, client.GetReader());
                            break;
                        case 3:  // Ping
                            await _utilsHandler.HandlePing(client.GetStream());
                            break;
                        case 4:  // Chat message
                            if (client.PlayerData.IsAuthenticated)
                                await _chatService.HandlePacket(client, client.GetReader());
                            break;
                        case 10: // Login
                            var loginResult = await _loginHandler.HandleLogin(client.GetStream(), client.GetReader(), _clients);
                            if (loginResult.Success && loginResult.User != null)
                            {
                                client.SetAuthenticated(loginResult.User.Username);
                                client.PlayerData.Position = new Position(loginResult.User.PositionX, loginResult.User.PositionY);
                                client.PlayerData.Direction = loginResult.User.Direction;
                                client.PlayerData.Rank = (byte)loginResult.User.Rank;
                                _clients.TryAdd(loginResult.User.Username, client);
                                await _playerHandler.SendPlayerSpawn(client, _clients);
                            }
                            break;

                        case 14: // Handshake
                            await _handshakeHandler.HandleHandshake(client.GetStream());
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