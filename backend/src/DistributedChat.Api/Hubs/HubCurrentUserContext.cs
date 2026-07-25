namespace DistributedChat.Api.Hubs;

public sealed class HubCurrentUserContext
{
    private readonly AsyncLocal<HubCurrentUser?> currentUser = new();

    public HubCurrentUser? Current => currentUser.Value;

    public IDisposable Use(Guid userId, string? username, string? email)
    {
        var previous = currentUser.Value;
        currentUser.Value = new HubCurrentUser(userId, username, email);

        return new RestoreCurrentUser(this, previous);
    }

    private sealed class RestoreCurrentUser(
        HubCurrentUserContext context,
        HubCurrentUser? previous
    ) : IDisposable
    {
        public void Dispose()
        {
            context.currentUser.Value = previous;
        }
    }
}

public sealed record HubCurrentUser(Guid UserId, string? Username, string? Email);
