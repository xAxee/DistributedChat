namespace DistributedChat.Domain.Users;

public sealed class User
{
    public const int MinimumUsernameLength = 3;
    public const int MaximumUsernameLength = 30;
    public const int MaximumEmailLength = 320;

    public Guid Id { get; set; }

    public required string Username { get; set; }

    public required string NormalizedUsername { get; set; }

    public required string Email { get; set; }

    public required string NormalizedEmail { get; set; }

    public required string PasswordHash { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
