using System.Globalization;
using System.Text.Json;
using DistributedChat.Application.Messages;
using DistributedChat.Infrastructure.Messaging;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DistributedChat.IntegrationTests.Messaging;

[Collection(TestCollections.Messaging)]
public sealed class RabbitMqChatEventPublisherTests(RabbitMqFixture rabbitMqFixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task PublishAsyncPublishesJsonEventWithRequiredRabbitMqProperties()
    {
        var rabbitMqOptions = rabbitMqFixture.CreateOptions();
        var queueName = $"publisher-test-{Guid.NewGuid():N}";

        using var receivingConnection = rabbitMqFixture.CreateConnection();
        using var receivingChannel = receivingConnection.CreateModel();
        RabbitMqTopology.DeclareExchange(receivingChannel, rabbitMqOptions);
        receivingChannel.QueueDeclare(
            queueName,
            durable: false,
            exclusive: false,
            autoDelete: true,
            arguments: null);
        receivingChannel.QueueBind(queueName, rabbitMqOptions.ExchangeName, routingKey: string.Empty);

        using var rabbitMqConnection = new RabbitMqConnection(
            Options.Create(rabbitMqOptions),
            NullLogger<RabbitMqConnection>.Instance);
        using var publisher = new RabbitMqChatEventPublisher(
            rabbitMqConnection,
            Options.Create(rabbitMqOptions));

        var messageCreated = CreateContract();

        await publisher.PublishMessageCreatedAsync(messageCreated);

        var received = await WaitForBasicGetAsync(receivingChannel, queueName);

        Assert.NotNull(received);
        Assert.Equal("application/json", received.BasicProperties.ContentType);
        Assert.Equal("utf-8", received.BasicProperties.ContentEncoding);
        Assert.Equal(messageCreated.EventId.ToString("D"), received.BasicProperties.MessageId);
        Assert.Equal(ChatEventTypes.MessageCreated, received.BasicProperties.Type);

        var deserialized = JsonSerializer.Deserialize<ChatMessageCreated>(received.Body.Span, JsonOptions);

        Assert.Equal(messageCreated, deserialized);
    }

    private static async Task<BasicGetResult> WaitForBasicGetAsync(
        IModel channel,
        string queueName
    )
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var result = channel.BasicGet(queueName, autoAck: true);
            if (result is not null)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException("Timed out waiting for RabbitMQ message.");
    }

    private static ChatMessageCreated CreateContract()
    {
        return new ChatMessageCreated(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Guid.Parse("44444444-4444-4444-4444-444444444444"),
            "alice",
            "Hello from RabbitMQ",
            DateTimeOffset.Parse("2026-07-10T13:30:45.123+00:00", CultureInfo.InvariantCulture));
    }
}
