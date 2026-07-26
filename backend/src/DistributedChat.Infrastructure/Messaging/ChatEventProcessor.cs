using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Domain.ProcessedEvents;
using DistributedChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Infrastructure.Messaging;

public sealed class ChatEventProcessor(
    DistributedChatDbContext dbContext,
    IChatClientNotifier chatClientNotifier,
    TimeProvider timeProvider
)
{
    public Task<ChatEventProcessingResult> ProcessAsync(
        string consumerId,
        ChatEventEnvelope envelope,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(consumerId);

        return ProcessOnceAsync(consumerId, envelope.EventId, envelope.NotifyAsync, cancellationToken);
    }

    private async Task<ChatEventProcessingResult> ProcessOnceAsync(
        string consumerId,
        Guid eventId,
        Func<IChatClientNotifier, CancellationToken, Task> notifyAsync,
        CancellationToken cancellationToken
    )
    {
        if (await IsProcessedAsync(consumerId, eventId, cancellationToken))
        {
            return ChatEventProcessingResult.Duplicate;
        }

        try
        {
            await notifyAsync(chatClientNotifier, cancellationToken);

            dbContext.ProcessedEvents.Add(new ProcessedEvent
            {
                ConsumerId = consumerId,
                EventId = eventId,
                ProcessedAt = timeProvider.GetUtcNow(),
            });

            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            if (await IsProcessedAsync(consumerId, eventId, cancellationToken))
            {
                return ChatEventProcessingResult.Duplicate;
            }

            throw;
        }

        return ChatEventProcessingResult.Processed;
    }

    private Task<bool> IsProcessedAsync(
        string consumerId,
        Guid eventId,
        CancellationToken cancellationToken
    )
    {
        return dbContext.ProcessedEvents
            .AsNoTracking()
            .AnyAsync(
                processedEvent => processedEvent.ConsumerId == consumerId
                    && processedEvent.EventId == eventId,
                cancellationToken);
    }
}
