namespace DistributedChat.Application.Presence;

public static class UserPresenceDefaults
{
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(15);

    public static readonly TimeSpan OfflineAfter = TimeSpan.FromSeconds(45);
}
