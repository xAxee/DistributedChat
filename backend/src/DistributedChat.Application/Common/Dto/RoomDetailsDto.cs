namespace DistributedChat.Application.Common.Dto;

public sealed record RoomDetailsDto(
    Guid Id,
    string Name,
    Guid CreatedByUserId,
    DateTimeOffset CreatedAt,
    bool IsMember,
    bool IsPrivate
);
