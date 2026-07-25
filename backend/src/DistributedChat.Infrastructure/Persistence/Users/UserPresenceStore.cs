using DistributedChat.Application.Common.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Infrastructure.Persistence.Users;

public sealed class UserPresenceStore(DistributedChatDbContext dbContext) : IUserPresenceStore
{
    public Task ConnectAsync(
        Guid userId,
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO user_presences (user_id, instance_id, connection_count, last_heartbeat_at, updated_at)
            VALUES ({userId}, {instanceId}, 1, {now}, {now})
            ON CONFLICT (user_id, instance_id)
            DO UPDATE SET
                connection_count = user_presences.connection_count + 1,
                last_heartbeat_at = EXCLUDED.last_heartbeat_at,
                updated_at = EXCLUDED.updated_at;
            """,
            cancellationToken);
    }

    public Task DisconnectAsync(
        Guid userId,
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"""
            UPDATE user_presences
            SET
                connection_count = GREATEST(connection_count - 1, 0),
                last_heartbeat_at = CASE WHEN connection_count > 1 THEN {now} ELSE last_heartbeat_at END,
                updated_at = {now}
            WHERE user_id = {userId}
              AND instance_id = {instanceId};
            """,
            cancellationToken);
    }

    public Task HeartbeatAsync(
        string instanceId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return dbContext.UserPresences
            .Where(presence => presence.InstanceId == instanceId && presence.ConnectionCount > 0)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(presence => presence.LastHeartbeatAt, now)
                    .SetProperty(presence => presence.UpdatedAt, now),
                cancellationToken);
    }

    public Task ClearInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(instanceId);

        return dbContext.UserPresences
            .Where(presence => presence.InstanceId == instanceId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
