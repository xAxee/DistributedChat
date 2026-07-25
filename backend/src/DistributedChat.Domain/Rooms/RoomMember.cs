namespace DistributedChat.Domain.Rooms;

public sealed class RoomMember
{
    public Guid RoomId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset JoinedAt { get; set; }
}
