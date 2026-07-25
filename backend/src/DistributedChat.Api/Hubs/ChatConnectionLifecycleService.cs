using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using DistributedChat.Infrastructure.Messaging;

namespace DistributedChat.Api.Hubs;

public sealed partial class ChatConnectionLifecycleService(
    IUserPresenceStore userPresenceStore,
    TimeProvider timeProvider,
    Microsoft.Extensions.Options.IOptions<InstanceOptions> instanceOptions,
    ConnectionRegistry connectionRegistry,
    IChatEventPublisher chatEventPublisher,
    Microsoft.Extensions.Logging.ILogger<ChatConnectionLifecycleService> logger
)
{
    private readonly string instanceId = instanceOptions.Value.InstanceId;

    public async Task ConnectAsync(
        string connectionId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await userPresenceStore.ConnectAsync(
            userId,
            instanceId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        connectionRegistry.Add(connectionId, userId);
    }

    public async Task<Guid?> DisconnectAsync(
        string connectionId,
        Guid? fallbackUserId,
        CancellationToken cancellationToken = default
    )
    {
        var connection = connectionRegistry.Remove(connectionId);
        var userId = connection?.UserId ?? fallbackUserId;

        if (userId is null)
        {
            return null;
        }

        await userPresenceStore.DisconnectAsync(
            userId.Value,
            instanceId,
            timeProvider.GetUtcNow(),
            cancellationToken);

        foreach (var roomId in connection?.RoomSubscriptions ?? [])
        {
            var presenceEvent = new UserRoomPresenceEvent(
                Guid.NewGuid(),
                roomId,
                userId.Value,
                null,
                connectionId,
                instanceId);

            try
            {
                await chatEventPublisher.PublishUserLeftRoomAsync(presenceEvent, cancellationToken);
            }
            catch (Exception exception)
            {
                LogPresencePublishFailed(
                    logger,
                    exception,
                    userId.Value,
                    roomId);
            }
        }

        return userId.Value;
    }

    [LoggerMessage(
        EventId = 1,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Could not publish UserLeftRoom presence event for user {UserId} in room {RoomId} during disconnect.")]
    private static partial void LogPresencePublishFailed(
        Microsoft.Extensions.Logging.ILogger logger,
        Exception exception,
        Guid userId,
        Guid roomId);
}
