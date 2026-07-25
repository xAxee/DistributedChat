namespace DistributedChat.Infrastructure.Messaging;

public sealed class MessagingOptions
{
    public const string SectionName = "Messaging";
    public const string InMemoryTransport = "InMemory";
    public const string RabbitMqTransport = "RabbitMq";

    public string Transport { get; set; } = InMemoryTransport;

    public bool IsRabbitMqTransport()
    {
        return string.Equals(Transport, RabbitMqTransport, StringComparison.OrdinalIgnoreCase);
    }

    public bool IsSupportedTransport()
    {
        return string.Equals(Transport, InMemoryTransport, StringComparison.OrdinalIgnoreCase)
            || IsRabbitMqTransport();
    }
}
