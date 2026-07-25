using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using DistributedChat.Application.Rooms;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace DistributedChat.Api.Hubs;

public sealed class SignalRChatClientNotifier(
    IHubContext<ChatHub> hubContext,
    ConnectionRegistry connectionRegistry,
    IServiceScopeFactory serviceScopeFactory
) : IChatClientNotifier
{
    public async Task NotifyMessageReceivedAsync(
        ChatMessageCreated messageCreated,
        CancellationToken cancellationToken = default
    )
    {
        using var eventScope = LogContext.PushProperty("EventId", messageCreated.EventId);
        using var messageScope = LogContext.PushProperty("MessageId", messageCreated.MessageId);
        using var roomScope = LogContext.PushProperty("RoomId", messageCreated.RoomId);
        using var userScope = LogContext.PushProperty("UserId", messageCreated.SenderUserId);

        var connectionIds = await GetAuthorizedConnectionIdsAsync(
            messageCreated.RoomId,
            cancellationToken);

        await hubContext.Clients
            .Clients(connectionIds)
            .SendAsync(ChatHubEvents.MessageReceived, messageCreated, cancellationToken);
    }

    public async Task NotifyUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default
    )
    {
        var connectionIds = await GetAuthorizedConnectionIdsAsync(
            userJoinedRoom.RoomId,
            cancellationToken);

        await hubContext.Clients
            .Clients(connectionIds)
            .SendAsync(ChatHubEvents.UserJoinedRoom, userJoinedRoom, cancellationToken);
    }

    public async Task NotifyUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default
    )
    {
        var connectionIds = await GetAuthorizedConnectionIdsAsync(
            userLeftRoom.RoomId,
            cancellationToken);

        await hubContext.Clients
            .Clients(connectionIds)
            .SendAsync(ChatHubEvents.UserLeftRoom, userLeftRoom, cancellationToken);
    }

    private async Task<IReadOnlyList<string>> GetAuthorizedConnectionIdsAsync(
        Guid roomId,
        CancellationToken cancellationToken
    )
    {
        var subscriptions = connectionRegistry.GetRoomSubscriptions(roomId);
        if (subscriptions.Count == 0)
        {
            return [];
        }

        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var roomStore = scope.ServiceProvider.GetRequiredService<IRoomStore>();
        var memberUserIds = await roomStore.GetMemberUserIdsAsync(roomId, cancellationToken);
        var memberUserIdSet = memberUserIds.ToHashSet();

        return subscriptions
            .Where(connection => memberUserIdSet.Contains(connection.UserId))
            .Select(connection => connection.ConnectionId)
            .ToArray();
    }
}
