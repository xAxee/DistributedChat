using System.Collections.Concurrent;

namespace DistributedChat.Api.Hubs;

public sealed class LocalSignalRSendMessageRateLimiter(TimeProvider timeProvider)
{
    private static readonly TimeSpan Window = TimeSpan.FromSeconds(10);
    private const int PermitLimit = 10;

    private readonly ConcurrentDictionary<Guid, Counter> counters = new();

    public bool TryAcquire(Guid userId)
    {
        var now = timeProvider.GetUtcNow();
        var counter = counters.GetOrAdd(userId, _ => new Counter(now));

        lock (counter)
        {
            if (now - counter.WindowStartedAt >= Window)
            {
                counter.WindowStartedAt = now;
                counter.Count = 0;
            }

            if (counter.Count >= PermitLimit)
            {
                return false;
            }

            counter.Count++;

            return true;
        }
    }

    private sealed class Counter(DateTimeOffset windowStartedAt)
    {
        public DateTimeOffset WindowStartedAt { get; set; } = windowStartedAt;

        public int Count { get; set; }
    }
}
