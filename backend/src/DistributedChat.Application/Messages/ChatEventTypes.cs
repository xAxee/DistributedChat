namespace DistributedChat.Application.Messages;

public static class ChatEventTypes
{
    public const string MessageCreated = "chat.message-created";
    public const string UserJoinedRoom = "chat.user-joined-room";
    public const string UserLeftRoom = "chat.user-left-room";
}
