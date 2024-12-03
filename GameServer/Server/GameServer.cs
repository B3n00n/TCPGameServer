using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Concurrent;
using GameServer.Config;
using GameServer.Handlers;
using GameServer.Packets;
using System.Buffers.Binary;

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
                // Add more...
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
                    Console.WriteLine("Waiting for packet...");
                    var packet = await ReadPacketAsync(client);

                    if (packet == null)
                    {
                        Console.WriteLine("Received null packet, breaking connection loop");
                        break;
                    }

                    Console.WriteLine($"Dispatching packet with opcode: {packet.Opcode}");
                    await _packetDispatcher.DispatchPacketAsync(client, packet);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
            finally
            {
                Console.WriteLine("Client disconnected");
                if (!string.IsNullOrEmpty(client.Username))
                {
                    _authService.RemoveOnlinePlayer(client.Username);
                    _clients.TryRemove(client.Username, out _);
                }
                client.Disconnect();
            }
        }

        private async Task<Packet?> ReadPacketAsync(GameClient client)
        {
            var opcodeByte = new byte[1];
            var lengthBytes = new byte[2];

            try
            {
                var stream = client.GetStream();

                // Read opcode
                if (await stream.ReadAsync(opcodeByte, 0, 1) != 1)
                    return null;

                // Read length for non-fixed size packets
                if (await stream.ReadAsync(lengthBytes, 0, 2) != 2)
                    return null;

                // Fix endianness handling
                var length = BinaryPrimitives.ReadUInt16BigEndian(lengthBytes);

                // Add size validation
                if (length > 5000) // Maximum reasonable packet size
                {
                    Console.WriteLine($"Received invalid packet length: {length}");
                    return null;
                }

                var payload = new byte[length];

                if (length > 0)
                {
                    var bytesRead = 0;
                    while (bytesRead < length)
                    {
                        var read = await stream.ReadAsync(payload, bytesRead, length - bytesRead);
                        if (read == 0) return null;
                        bytesRead += read;
                    }
                }

                // Add logging
                Console.WriteLine($"Received packet - Opcode: {opcodeByte[0]}, Length: {length}");

                return new Packet(opcodeByte[0], payload);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading packet: {ex.Message}");
                return null;
            }
        }
    }
}
