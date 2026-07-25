namespace DistributedChat.Api.Hubs;

public sealed record ChatHubUser(Guid UserId, string? Username, string? Email);
