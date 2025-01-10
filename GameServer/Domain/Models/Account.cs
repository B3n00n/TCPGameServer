public class Account
{
    public int Id { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
    public int Rank { get; set; }
    public bool IsBanned { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}