namespace DistributedChat.Domain.Users;

public sealed class UserPresence
{
    public const int MaximumInstanceIdLength = 128;

    public Guid UserId { get; set; }

    public required string InstanceId { get; set; }

    public int ConnectionCount { get; set; }

    public DateTimeOffset LastHeartbeatAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
