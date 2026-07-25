namespace DistributedChat.Infrastructure.Messaging;

public sealed class InvalidChatEventException : Exception
{
    public InvalidChatEventException(string message)
        : base(message)
    {
    }

    public InvalidChatEventException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}