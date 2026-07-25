namespace DistributedChat.Api.Hubs;

public static class ChatHubGroups
{
    public static string Room(Guid roomId) => $"room:{roomId}";
}
