using GameServer.Core.Network;
using GameServer.Domain;
using GameServer.Domain.Models;
using System.Collections.Concurrent;
using System.Net.Sockets;

namespace GameServer.Handlers
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

    public class LoginPacketHandler
    {
        private readonly UserService _userService;

        public LoginPacketHandler(UserService userService)
        {
            _userService = userService;
        }

        public async Task<(LoginType Status, Account? Account, AccountState? State, AccountVisuals? Visuals)> HandleLogin(NetworkStream stream, PacketReader readBuffer, ConcurrentDictionary<string, GameClient> activeClients)
        {
            var revision = await readBuffer.ReadU32();
            var username = await readBuffer.ReadString();
            var password = await readBuffer.ReadString();

            var result = await _userService.AuthenticateAsync(username, password, revision, activeClients);

            var response = new PacketWriter();
            response.WriteU8((byte)result.Status);

            if (result.Status == LoginType.ACCEPTABLE && result.Account != null)
            {
                response.WriteU8((byte)result.Account.Rank);
            }

            await stream.WriteAsync(response.ToArray());

            return result;
        }
    }
}