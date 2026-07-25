using DistributedChat.Application.Messages;

namespace DistributedChat.Application.Common.Abstractions;

public interface IChatEventPublisher
{
    Task PublishMessageCreatedAsync(
        ChatMessageCreated messageCreated,
        CancellationToken cancellationToken = default);

    Task PublishUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default);

    Task PublishUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default);
}
