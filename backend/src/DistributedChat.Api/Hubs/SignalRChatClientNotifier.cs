using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using Microsoft.AspNetCore.SignalR;
using Serilog.Context;

namespace DistributedChat.Api.Hubs;

public sealed class SignalRChatClientNotifier(
    IHubContext<ChatHub> hubContext
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

        await hubContext.Clients
            .Group(ChatHubGroups.Room(messageCreated.RoomId))
            .SendAsync(ChatHubEvents.MessageReceived, messageCreated, cancellationToken);
    }

    public async Task NotifyUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default
    )
    {
        await hubContext.Clients
            .Group(ChatHubGroups.Room(userJoinedRoom.RoomId))
            .SendAsync(ChatHubEvents.UserJoinedRoom, userJoinedRoom, cancellationToken);
    }

    public async Task NotifyUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default
    )
    {
        await hubContext.Clients
            .Group(ChatHubGroups.Room(userLeftRoom.RoomId))
            .SendAsync(ChatHubEvents.UserLeftRoom, userLeftRoom, cancellationToken);
    }
}
