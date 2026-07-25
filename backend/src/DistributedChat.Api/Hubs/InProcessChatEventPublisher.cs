using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using Serilog.Context;

namespace DistributedChat.Api.Hubs;

public sealed class InProcessChatEventPublisher(
    IChatClientNotifier chatClientNotifier
) : IChatEventPublisher
{
    public async Task PublishMessageCreatedAsync(
        ChatMessageCreated messageCreated,
        CancellationToken cancellationToken = default
    )
    {
        using var eventScope = LogContext.PushProperty("EventId", messageCreated.EventId);
        using var messageScope = LogContext.PushProperty("MessageId", messageCreated.MessageId);
        using var roomScope = LogContext.PushProperty("RoomId", messageCreated.RoomId);
        using var userScope = LogContext.PushProperty("UserId", messageCreated.SenderUserId);

        await chatClientNotifier.NotifyMessageReceivedAsync(messageCreated, cancellationToken);
    }

    public Task PublishUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default
    )
    {
        return chatClientNotifier.NotifyUserJoinedRoomAsync(userJoinedRoom, cancellationToken);
    }

    public Task PublishUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default
    )
    {
        return chatClientNotifier.NotifyUserLeftRoomAsync(userLeftRoom, cancellationToken);
    }
}
