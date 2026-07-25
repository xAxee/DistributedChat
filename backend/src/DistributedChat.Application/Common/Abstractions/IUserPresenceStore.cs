namespace DistributedChat.Application.Common.Abstractions;

public interface IUserPresenceStore
{
    Task ConnectAsync(
        Guid userId,
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task DisconnectAsync(
        Guid userId,
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task HeartbeatAsync(
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task ClearInstanceAsync(
        string instanceId,
        CancellationToken cancellationToken = default);
}
