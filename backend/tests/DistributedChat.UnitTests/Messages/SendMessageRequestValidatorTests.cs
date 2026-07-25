using DistributedChat.Application.Messages;

namespace DistributedChat.UnitTests.Messages;

public sealed class SendMessageRequestValidatorTests
{
    private readonly SendMessageRequestValidator validator = new();

    [Fact]
    public async Task AcceptsValidRequest()
    {
        var result = await validator.ValidateAsync(new SendMessageRequest(Guid.NewGuid(), "  hello  "));

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task RejectsEmptyRoomId()
    {
        var result = await validator.ValidateAsync(new SendMessageRequest(Guid.Empty, "hello"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SendMessageRequest.RoomId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RejectsMissingOrWhitespaceContent(string? content)
    {
        var result = await validator.ValidateAsync(new SendMessageRequest(Guid.NewGuid(), content));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SendMessageRequest.Content));
    }

    [Fact]
    public async Task RejectsContentLongerThan2000Characters()
    {
        var result = await validator.ValidateAsync(new SendMessageRequest(Guid.NewGuid(), new string('a', 2001)));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(SendMessageRequest.Content));
    }
}
