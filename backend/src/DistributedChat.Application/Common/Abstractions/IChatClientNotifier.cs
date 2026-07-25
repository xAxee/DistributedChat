using DistributedChat.Application.Messages;

namespace DistributedChat.Application.Common.Abstractions;

public interface IChatClientNotifier
{
    Task NotifyMessageReceivedAsync(
        ChatMessageCreated messageCreated,
        CancellationToken cancellationToken = default);

    Task NotifyUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default);

    Task NotifyUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default);
}
