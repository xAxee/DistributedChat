namespace DistributedChat.Application.Messages;

public sealed record SendMessageRequest(Guid RoomId, string? Content);
