namespace DistributedChat.Api.Hubs;

public static class ChatHubEvents
{
    public const string MessageReceived = nameof(MessageReceived);
    public const string UserJoinedRoom = nameof(UserJoinedRoom);
    public const string UserLeftRoom = nameof(UserLeftRoom);
}
