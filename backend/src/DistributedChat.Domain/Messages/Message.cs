namespace DistributedChat.Domain.Messages;

public sealed class Message
{
    public const int MinimumContentLength = 1;
    public const int MaximumContentLength = 2000;

    private Message()
    {
    }

    public Guid Id { get; private set; }

    public Guid RoomId { get; private set; }

    public Guid SenderUserId { get; private set; }

    public string Content { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; private set; }

    public static Message Create(
        Guid id,
        Guid roomId,
        Guid senderUserId,
        string content,
        DateTimeOffset createdAt
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Message id is required.", nameof(id));
        }

        if (roomId == Guid.Empty)
        {
            throw new ArgumentException("Room id is required.", nameof(roomId));
        }

        if (senderUserId == Guid.Empty)
        {
            throw new ArgumentException("Sender user id is required.", nameof(senderUserId));
        }

        var normalizedContent = NormalizeContent(content);

        return new Message
        {
            Id = id,
            RoomId = roomId,
            SenderUserId = senderUserId,
            Content = normalizedContent,
            CreatedAt = createdAt,
        };
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Message content is required.", nameof(content));
        }

        var normalized = content.Trim();
        if (normalized.Length is < MinimumContentLength or > MaximumContentLength)
        {
            throw new ArgumentException(
                $"Message content must be between {MinimumContentLength} and {MaximumContentLength} characters.",
                nameof(content));
        }

        return normalized;
    }
}
