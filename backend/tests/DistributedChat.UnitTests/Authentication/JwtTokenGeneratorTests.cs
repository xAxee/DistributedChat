using System.IdentityModel.Tokens.Jwt;
using DistributedChat.Domain.Users;
using DistributedChat.Infrastructure.Authentication;
using Microsoft.Extensions.Options;

namespace DistributedChat.UnitTests.Authentication;

public sealed class JwtTokenGeneratorTests
{
    [Fact]
    public void GeneratesExpectedClaims()
    {
        var userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var generator = new JwtTokenGenerator(
            Options.Create(
                new JwtOptions
                {
                    Issuer = "DistributedChat.Tests",
                    Audience = "DistributedChat.Tests.Api",
                    SigningKey = "test-signing-key-with-at-least-32-characters",
                    ExpirationMinutes = 30,
                }),
            new FixedTimeProvider(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero)));

        var token = generator.GenerateToken(
            new User
            {
                Id = userId,
                Username = "alice",
                NormalizedUsername = "ALICE",
                Email = "alice@example.com",
                NormalizedEmail = "ALICE@EXAMPLE.COM",
                PasswordHash = "hash",
                CreatedAt = DateTimeOffset.UtcNow,
            });

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Equal("DistributedChat.Tests", jwt.Issuer);
        Assert.Contains("DistributedChat.Tests.Api", jwt.Audiences);
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == userId.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.UniqueName && claim.Value == "alice");
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "alice@example.com");
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Jti && Guid.TryParse(claim.Value, out _));
        Assert.Equal(new DateTimeOffset(2026, 7, 10, 12, 30, 0, TimeSpan.Zero), token.ExpiresAt);
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
