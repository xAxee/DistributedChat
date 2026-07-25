namespace DistributedChat.Api.Dtos;

public sealed record CreateRoomDto(string? Name, bool IsPrivate = false, string? Password = null);
