namespace DistributedChat.Application.Common.Dto;

public sealed record RoomSummaryDto(Guid Id, string Name, Guid CreatedByUserId, DateTimeOffset CreatedAt);
