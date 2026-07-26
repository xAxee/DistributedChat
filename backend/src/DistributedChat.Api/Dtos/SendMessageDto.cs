namespace DistributedChat.Api.Dtos;

public sealed record SendMessageDto(Guid RoomId, string? Content);
