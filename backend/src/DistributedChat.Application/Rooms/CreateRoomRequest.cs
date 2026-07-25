namespace DistributedChat.Application.Rooms;

public sealed record CreateRoomRequest(string? Name, bool IsPrivate, string? Password);
