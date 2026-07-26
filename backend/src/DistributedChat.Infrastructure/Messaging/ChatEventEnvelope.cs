using DistributedChat.Application.Common.Abstractions;

namespace DistributedChat.Infrastructure.Messaging;

public sealed class ChatEventEnvelope(
    Guid eventId,
    Func<IChatClientNotifier, CancellationToken, Task> notifyAsync
)
{
    public Guid EventId { get; } = eventId;

    public Func<IChatClientNotifier, CancellationToken, Task> NotifyAsync { get; } = notifyAsync;
}
