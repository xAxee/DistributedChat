namespace DistributedChat.Application.Messages;

public sealed record ChatMessageCreated(
    Guid EventId,
    Guid MessageId,
    Guid RoomId,
    Guid SenderUserId,
    string SenderUsername,
    string Content,
    DateTimeOffset CreatedAt
);