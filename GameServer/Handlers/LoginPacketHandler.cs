using GameServer.Config;
using GameServer.Packets;

namespace GameServer
{
    public enum LoginType
    {
        ACCEPTABLE = 0,
        UNUSED = 1,
        LOGGED_IN = 2,
        INVALID_CREDENTIALS = 3,
        ACCOUNT_BANNED = 4,
        ALREADY_ONLINE = 5,
        REVISION_MISMATCH = 6,
        WORLD_FULL = 7,
        SERVER_OFFLINE = 8,
        MAX_PLAYERS = 9,
        BAD_SESSION_ID = 10,
        ACCOUNT_HACKED = 11,
        MEMBER_WORLD = 12,
        COULD_NOT_COMPLETE_LOGIN = 13,
        SERVER_UPDATE = 14,
        UNUSED_2 = 15,
        MAX_ATTEMPTS = 16,
        UNUSED_3 = 17,
        ACCOUNT_LOCKED = 18,
        NEW_ACCOUNT = 19,
        USERNAME_TOO_LONG = 20,
        NO_PERMISSIONS = 21,
        LOGIN_SERVER_OFFLINE = 22
    }

    public class LoginPacketHandler : IPacketHandler
    {
        private static readonly byte[] HandledOpcodes = { 10 };
        private readonly AuthService _authService;

        public LoginPacketHandler(AuthService authService)
        {
            _authService = authService;
        }

        public IEnumerable<byte> GetHandledOpcodes() => HandledOpcodes;

        public async Task HandlePacketAsync(GameClient client, Packet packet)
        {
            if (packet.Opcode == 10)
            {
                await HandleLoginRequest(client, packet);
            }
        }

        private async Task HandleLoginRequest(GameClient client, Packet packet)
        {
            try
            {
                var buffer = new StreamBuffer(client.GetStream());

                var revision = await buffer.ReadU32();
                var username = await buffer.ReadString();
                var password = await buffer.ReadString();

                Console.WriteLine($"Login attempt from '{username}' with revision {revision}");

                if (revision != GameConfig.REVISION)
                {
                    Console.WriteLine($"Revision mismatch. Expected {GameConfig.REVISION}, got {revision}");
                    await SendLoginResponse(client, LoginType.REVISION_MISMATCH);
                    return;
                }

                var (status, user) = await _authService.AuthenticateAsync(username, password);
                Console.WriteLine($"Login status for '{username}': {status}");

                if (status == LoginType.ACCEPTABLE && user != null)
                {
                    client.Username = username;
                    client.IsAuthenticated = true;

                    var response = new StreamBuffer();
                    response.WriteU8((byte)LoginType.ACCEPTABLE);
                    response.WriteU8((byte)user.Rank);
                    await client.SendPacketAsync(response.ToArray());
                }
                else
                {
                    await SendLoginResponse(client, status);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling login: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                await SendLoginResponse(client, LoginType.COULD_NOT_COMPLETE_LOGIN);
            }
        }

        private async Task SendLoginResponse(GameClient client, LoginType type)
        {
            var buffer = new StreamBuffer();
            buffer.WriteU8((byte)type);
            await client.SendPacketAsync(buffer.ToArray());
        }
    }
}