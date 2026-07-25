namespace DistributedChat.Domain.ProcessedEvents;

public sealed class ProcessedEvent
{
    public required string ConsumerId { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset ProcessedAt { get; set; }
}
