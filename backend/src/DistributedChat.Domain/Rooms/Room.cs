namespace DistributedChat.Domain.Rooms;

public sealed class Room
{
    public const int MinimumNameLength = 3;
    public const int MaximumNameLength = 50;

    private Room()
    {
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public Guid CreatedByUserId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public static Room Create(
        Guid id,
        string name,
        Guid createdByUserId,
        DateTimeOffset createdAt
    )
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Room id is required.", nameof(id));
        }

        if (createdByUserId == Guid.Empty)
        {
            throw new ArgumentException("Room creator id is required.", nameof(createdByUserId));
        }

        var normalizedName = NormalizeName(name);

        return new Room
        {
            Id = id,
            Name = normalizedName,
            CreatedByUserId = createdByUserId,
            CreatedAt = createdAt,
        };
    }

    private static string NormalizeName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Room name is required.", nameof(name));
        }

        var normalized = name.Trim();
        if (normalized.Length is < MinimumNameLength or > MaximumNameLength)
        {
            throw new ArgumentException(
                $"Room name must be between {MinimumNameLength} and {MaximumNameLength} characters.",
                nameof(name));
        }

        return normalized;
    }
}
