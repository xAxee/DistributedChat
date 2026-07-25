using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using DistributedChat.Application.Rooms;
using DistributedChat.Infrastructure.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace DistributedChat.Api.Hubs;

public sealed partial class ChatRoomSubscriptionService(
    IRoomStore roomStore,
    ConnectionRegistry connectionRegistry,
    IChatEventPublisher chatEventPublisher,
    Microsoft.Extensions.Options.IOptions<InstanceOptions> instanceOptions,
    Microsoft.Extensions.Logging.ILogger<ChatRoomSubscriptionService> logger
)
{
    private readonly string instanceId = instanceOptions.Value.InstanceId;

    public async Task JoinRoomAsync(
        string connectionId,
        ChatHubUser user,
        Guid roomId,
        IGroupManager groups,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureRoomMembershipAsync(roomId, user.UserId, cancellationToken);

        if (!connectionRegistry.TryAddRoomSubscription(connectionId, roomId))
        {
            return;
        }

        try
        {
            await groups.AddToGroupAsync(
                connectionId,
                ChatHubGroups.Room(roomId),
                cancellationToken);
        }
        catch
        {
            connectionRegistry.TryRemoveRoomSubscription(connectionId, roomId);
            throw;
        }

        await PublishPresenceBestEffortAsync(
            () => chatEventPublisher.PublishUserJoinedRoomAsync(
                CreatePresenceEvent(roomId, user, connectionId),
                cancellationToken),
            "UserJoinedRoom",
            roomId,
            user.UserId);
    }

    public async Task LeaveRoomAsync(
        string connectionId,
        ChatHubUser user,
        Guid roomId,
        IGroupManager groups,
        CancellationToken cancellationToken = default
    )
    {
        await EnsureRoomMembershipAsync(roomId, user.UserId, cancellationToken);

        if (!connectionRegistry.TryRemoveRoomSubscription(connectionId, roomId))
        {
            return;
        }

        try
        {
            await groups.RemoveFromGroupAsync(
                connectionId,
                ChatHubGroups.Room(roomId),
                cancellationToken);
        }
        catch
        {
            connectionRegistry.TryAddRoomSubscription(connectionId, roomId);
            throw;
        }

        await PublishPresenceBestEffortAsync(
            () => chatEventPublisher.PublishUserLeftRoomAsync(
                CreatePresenceEvent(roomId, user, connectionId),
                cancellationToken),
            "UserLeftRoom",
            roomId,
            user.UserId);
    }

    private async Task EnsureRoomMembershipAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        if (!await roomStore.ExistsAsync(roomId, cancellationToken))
        {
            throw HubExceptionMapper.ToHubException(RoomErrors.NotFound());
        }

        if (!await roomStore.IsMemberAsync(roomId, userId, cancellationToken))
        {
            throw HubExceptionMapper.ToHubException(RoomErrors.MembershipRequired());
        }
    }

    private UserRoomPresenceEvent CreatePresenceEvent(
        Guid roomId,
        ChatHubUser user,
        string connectionId
    )
    {
        return new UserRoomPresenceEvent(
            Guid.NewGuid(),
            roomId,
            user.UserId,
            user.Username,
            connectionId,
            instanceId);
    }

    private async Task PublishPresenceBestEffortAsync(
        Func<Task> publish,
        string eventName,
        Guid roomId,
        Guid userId)
    {
        try
        {
            await publish();
        }
        catch (Exception exception)
        {
            LogPresencePublishFailed(
                logger,
                exception,
                eventName,
                userId,
                roomId);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = Microsoft.Extensions.Logging.LogLevel.Warning,
        Message = "Could not publish {EventName} presence event for user {UserId} in room {RoomId}.")]
    private static partial void LogPresencePublishFailed(
        Microsoft.Extensions.Logging.ILogger logger,
        Exception exception,
        string eventName,
        Guid userId,
        Guid roomId);
}
