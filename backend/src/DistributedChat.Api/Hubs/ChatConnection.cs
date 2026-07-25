namespace DistributedChat.Api.Hubs;

public sealed record ChatConnection(
    string ConnectionId,
    Guid UserId,
    IReadOnlyCollection<Guid> RoomSubscriptions);
