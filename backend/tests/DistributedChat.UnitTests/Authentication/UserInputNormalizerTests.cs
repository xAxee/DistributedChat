using DistributedChat.Application.Authentication;

namespace DistributedChat.UnitTests.Authentication;

public sealed class UserInputNormalizerTests
{
    [Fact]
    public void NormalizesEmailAndUsername()
    {
        var email = UserInputNormalizer.NormalizeEmail("  Alice@Example.com  ");
        var username = UserInputNormalizer.NormalizeUsername("  Alice  ");

        Assert.Equal("Alice@Example.com", email);
        Assert.Equal("Alice", username);
        Assert.Equal("ALICE@EXAMPLE.COM", UserInputNormalizer.ToLookupKey(email));
        Assert.Equal("ALICE", UserInputNormalizer.ToLookupKey(username));
    }
}
