namespace DistributedChat.Application.Messages;

public sealed record UserRoomPresenceEvent(
    Guid EventId,
    Guid RoomId,
    Guid UserId,
    string? Username,
    string ConnectionId,
    string InstanceId
);
