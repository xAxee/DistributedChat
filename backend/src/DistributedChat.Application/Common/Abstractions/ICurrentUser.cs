namespace DistributedChat.Application.Common.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    string? Username { get; }

    string? Email { get; }
}
