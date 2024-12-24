using GameServer.Domain.Models;
using GameServer.Handlers;
/// <summary>
/// Represents the result of a login attempt, encapsulating both success/failure and user data.
/// Immutable and created via factory methods to ensure valid state.
/// </summary>

namespace GameServer.Domain
{
    public class LoginResult
    {
        public bool Success { get; }
        public LoginType Status { get; }
        public User? User { get; }

        public LoginResult(LoginType status, User? user = null)
        {
            Status = status;
            User = user;
            Success = status == LoginType.ACCEPTABLE;
        }
    }
}