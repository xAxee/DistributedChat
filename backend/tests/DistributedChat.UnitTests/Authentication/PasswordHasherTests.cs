using DistributedChat.Infrastructure.Authentication;

namespace DistributedChat.UnitTests.Authentication;

public sealed class PasswordHasherTests
{
    [Fact]
    public void HashesAndVerifiesPassword()
    {
        var hasher = new PasswordHasher();

        var hash = hasher.HashPassword("correct-password");

        Assert.NotEqual("correct-password", hash);
        Assert.True(hasher.VerifyPassword(hash, "correct-password"));
        Assert.False(hasher.VerifyPassword(hash, "wrong-password"));
    }
}
