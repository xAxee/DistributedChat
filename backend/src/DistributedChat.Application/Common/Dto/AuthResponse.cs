namespace DistributedChat.Application.Common.Dto;

public sealed record AuthResponse(string AccessToken, DateTimeOffset ExpiresAt, CurrentUserDto User);
