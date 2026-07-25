namespace DistributedChat.Application.Authentication;

public sealed record RegisterRequest(string? Email, string? Username, string? Password);
