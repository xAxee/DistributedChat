using System.Collections.Concurrent;

namespace DistributedChat.Api.Hubs;

public sealed class ConnectionRegistry
{
    private readonly ConcurrentDictionary<string, ChatConnectionState> connections = new(StringComparer.Ordinal);

    public IReadOnlyCollection<ChatConnection> ActiveConnections => connections
        .Select(connection => new ChatConnection(
            connection.Key,
            connection.Value.UserId,
            connection.Value.RoomSubscriptions))
        .ToArray();

    public IReadOnlyCollection<Guid> ConnectedUsers => connections
        .Values
        .Select(connection => connection.UserId)
        .Distinct()
        .ToArray();

    public int ActiveConnectionCount => connections.Count;

    public int ConnectedUserCount => connections
        .Values
        .Select(connection => connection.UserId)
        .Distinct()
        .Count();

    public void Add(string connectionId, Guid userId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        connections[connectionId] = new ChatConnectionState(userId);
    }

    public ChatConnection? Remove(string connectionId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return connections.TryRemove(connectionId, out var connection)
            ? new ChatConnection(connectionId, connection.UserId, connection.RoomSubscriptions)
            : null;
    }

    public bool TryAddRoomSubscription(string connectionId, Guid roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        if (!connections.TryGetValue(connectionId, out var connection))
        {
            return false;
        }

        if (!connection.AddRoomSubscription(roomId))
        {
            return false;
        }

        if (connections.TryGetValue(connectionId, out var currentConnection)
            && ReferenceEquals(connection, currentConnection))
        {
            return true;
        }

        connection.RemoveRoomSubscription(roomId);

        return false;
    }

    public bool TryRemoveRoomSubscription(string connectionId, Guid roomId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        return connections.TryGetValue(connectionId, out var connection)
            && connection.RemoveRoomSubscription(roomId);
    }

    private sealed class ChatConnectionState(Guid userId)
    {
        private readonly ConcurrentDictionary<Guid, byte> roomSubscriptions = new();

        public Guid UserId { get; } = userId;

        public IReadOnlyCollection<Guid> RoomSubscriptions => roomSubscriptions.Keys.ToArray();

        public bool AddRoomSubscription(Guid roomId)
        {
            return roomSubscriptions.TryAdd(roomId, 0);
        }

        public bool RemoveRoomSubscription(Guid roomId)
        {
            return roomSubscriptions.TryRemove(roomId, out _);
        }
    }
}
