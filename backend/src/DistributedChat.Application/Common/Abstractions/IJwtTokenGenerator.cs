using DistributedChat.Domain.Users;

namespace DistributedChat.Application.Common.Abstractions;

public interface IJwtTokenGenerator
{
    GeneratedJwtToken GenerateToken(User user);
}

public sealed record GeneratedJwtToken(string AccessToken, DateTimeOffset ExpiresAt);
