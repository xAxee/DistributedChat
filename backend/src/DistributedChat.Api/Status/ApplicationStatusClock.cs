namespace DistributedChat.Api.Status;

public sealed class ApplicationStatusClock
{
    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;

    public long UptimeSeconds => (long)Math.Max(0, (DateTimeOffset.UtcNow - StartedAt).TotalSeconds);
}
