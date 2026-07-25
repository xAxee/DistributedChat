namespace DistributedChat.Application.Common.Dto;

public sealed record MessageDto(
    Guid Id,
    Guid RoomId,
    Guid SenderUserId,
    string SenderUsername,
    string Content,
    DateTimeOffset CreatedAt
);
