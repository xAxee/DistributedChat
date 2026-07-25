namespace DistributedChat.Api.Status;

public sealed record ApplicationStatusResponse(
    string InstanceId,
    int ActiveConnections,
    int ConnectedUsers,
    long UptimeSeconds,
    DateTimeOffset StartedAt,
    string ApplicationVersion);
