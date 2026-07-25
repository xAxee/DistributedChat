using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DistributedChat.Api.Dtos;
using DistributedChat.Api.Hubs;
using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Messages;
using DistributedChat.Infrastructure.Messaging;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.IntegrationTests.Messaging;

[Collection(TestCollections.Messaging)]
public sealed class RabbitMqChatEventConsumerTests(
    PostgreSqlFixture postgreSqlFixture,
    RabbitMqFixture rabbitMqFixture
) : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task InitializeAsync()
    {
        return postgreSqlFixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task RabbitMqTransportSendsMessageReceivedToLocalSignalRGroup()
    {
        const string instanceId = "consumer-e2e";
        var queueName = $"chat.instance.{instanceId}";

        await using var factory = new DistributedChatRabbitMqApiFactory(
            postgreSqlFixture,
            rabbitMqFixture,
            instanceId);
        using var client = factory.CreateClient();
        UseToken(client, (await RegisterAsync(client, "alice@example.com", "alice")).AccessToken);
        var room = await CreateRoomAsync(client, "general");

        await rabbitMqFixture.WaitForQueueAsync(queueName);

        await using var connection = CreateHubConnection(factory, client, client.DefaultRequestHeaders.Authorization!.Parameter!);
        var messageReceived = new TaskCompletionSource<ChatMessageCreated>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<ChatMessageCreated>(
            ChatHubEvents.MessageReceived,
            message => messageReceived.TrySetResult(message));

        await connection.StartAsync();
        await connection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);
        await connection.InvokeAsync(
            nameof(ChatHub.SendMessage),
            new SendMessageDto(room.Id, "hello through rabbitmq"));

        var received = await WaitForAsync(messageReceived.Task);

        Assert.Equal(room.Id, received.RoomId);
        Assert.Equal("alice", received.SenderUsername);
        Assert.Equal("hello through rabbitmq", received.Content);

        await using var dbContext = postgreSqlFixture.CreateDbContext();
        var processedEvent = await dbContext.ProcessedEvents.AsNoTracking().SingleAsync(processedEvent =>
            processedEvent.ConsumerId == instanceId && processedEvent.EventId == received.EventId);
        Assert.Equal(instanceId, processedEvent.ConsumerId);
        Assert.Equal(received.EventId, processedEvent.EventId);
    }

    [Fact]
    public async Task ProcessAsyncIsIdempotentForSameConsumerAndAllowsDifferentConsumers()
    {
        var messageCreated = CreateContract();
        await using var dbContext = postgreSqlFixture.CreateDbContext();
        var notifier = new CapturingChatMessageClientNotifier();
        var processor = new ChatEventProcessor(
            dbContext,
            notifier,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-07-11T09:00:00+00:00", CultureInfo.InvariantCulture)));

        var envelope = CreateEnvelope(messageCreated);
        var firstResult = await processor.ProcessAsync("chat.instance.a", envelope);
        var duplicateResult = await processor.ProcessAsync("chat.instance.a", envelope);
        var otherConsumerResult = await processor.ProcessAsync("chat.instance.b", envelope);

        Assert.Equal(ChatEventProcessingResult.Processed, firstResult);
        Assert.Equal(ChatEventProcessingResult.Duplicate, duplicateResult);
        Assert.Equal(ChatEventProcessingResult.Processed, otherConsumerResult);
        Assert.Equal(2, notifier.NotificationCount);
        Assert.Equal(2, await dbContext.ProcessedEvents.CountAsync());
    }

    [Fact]
    public async Task FailedNotificationCanBeRetried()
    {
        var messageCreated = CreateContract();
        await using var dbContext = postgreSqlFixture.CreateDbContext();
        var notifier = new FailingOnceChatMessageClientNotifier();
        var processor = new ChatEventProcessor(
            dbContext,
            notifier,
            new FixedTimeProvider(DateTimeOffset.Parse("2026-07-11T09:00:00+00:00", CultureInfo.InvariantCulture)));
        var envelope = CreateEnvelope(messageCreated);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => processor.ProcessAsync("chat.instance.a", envelope));

        Assert.Empty(await dbContext.ProcessedEvents.AsNoTracking().ToArrayAsync());

        var retryResult = await processor.ProcessAsync("chat.instance.a", envelope);

        Assert.Equal(ChatEventProcessingResult.Processed, retryResult);
        Assert.Equal(2, notifier.Attempts);
        Assert.Equal(1, await dbContext.ProcessedEvents.CountAsync());
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("{\"eventId\":\"00000000-0000-0000-0000-000000000000\"}")]
    public async Task ConsumerRejectsInvalidJsonOrInvalidPayloadWithoutRequeue(string payload)
    {
        const string instanceId = "consumer-invalid-payload";
        var queueName = $"chat.instance.{instanceId}";
        var options = rabbitMqFixture.CreateOptions();

        await using var factory = new DistributedChatRabbitMqApiFactory(
            postgreSqlFixture,
            rabbitMqFixture,
            instanceId);
        _ = factory.CreateClient();

        await rabbitMqFixture.WaitForQueueAsync(queueName);

        using (var publisherConnection = rabbitMqFixture.CreateConnection())
        using (var channel = publisherConnection.CreateModel())
        {
            RabbitMqTopology.DeclareExchange(channel, options);
            var properties = channel.CreateBasicProperties();
            properties.ContentType = "application/json";

            channel.BasicPublish(
                options.ExchangeName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: Encoding.UTF8.GetBytes(payload));
        }

        await rabbitMqFixture.WaitForQueueReadyMessagesAsync(queueName, expectedReadyMessages: 0);

        await using var dbContext = postgreSqlFixture.CreateDbContext();
        Assert.Empty(await dbContext.ProcessedEvents.AsNoTracking().ToArrayAsync());
    }

    private static void UseToken(HttpClient client, string accessToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private static async Task<AuthResponse> RegisterAsync(
        HttpClient client,
        string email,
        string username
    )
    {
        var response = await client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto(email, username, "password123"));

        return await ReadSuccessAsync<AuthResponse>(response);
    }

    private static async Task<RoomDetailsDto> CreateRoomAsync(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/rooms", new CreateRoomDto(name));

        return await ReadSuccessAsync<RoomDetailsDto>(response);
    }

    private static HubConnection CreateHubConnection(
        DistributedChatRabbitMqApiFactory factory,
        HttpClient client,
        string accessToken
    )
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(client.BaseAddress!, "/hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);

        return JsonSerializer.Deserialize<T>(content, JsonOptions)
            ?? throw new InvalidOperationException("Response body was empty.");
    }

    private static async Task<T> WaitForAsync<T>(Task<T> task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(10)));
        if (completedTask != task)
        {
            throw new TimeoutException("Timed out waiting for SignalR event.");
        }

        return await task;
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

    private static ChatEventEnvelope CreateEnvelope(ChatMessageCreated messageCreated)
    {
        return new ChatEventEnvelope(
            messageCreated.EventId,
            (notifier, cancellationToken) => notifier.NotifyMessageReceivedAsync(messageCreated, cancellationToken));
    }

    private sealed class CapturingChatMessageClientNotifier : IChatClientNotifier
    {
        public int NotificationCount { get; private set; }

        public Task NotifyMessageReceivedAsync(
            ChatMessageCreated messageCreated,
            CancellationToken cancellationToken = default
        )
        {
            NotificationCount++;

            return Task.CompletedTask;
        }

        public Task NotifyUserJoinedRoomAsync(
            UserRoomPresenceEvent userJoinedRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task NotifyUserLeftRoomAsync(
            UserRoomPresenceEvent userLeftRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }

    private sealed class FailingOnceChatMessageClientNotifier : IChatClientNotifier
    {
        public int Attempts { get; private set; }

        public Task NotifyMessageReceivedAsync(
            ChatMessageCreated messageCreated,
            CancellationToken cancellationToken = default
        )
        {
            Attempts++;

            return Attempts == 1
                ? Task.FromException(new InvalidOperationException("SignalR notification failed."))
                : Task.CompletedTask;
        }

        public Task NotifyUserJoinedRoomAsync(
            UserRoomPresenceEvent userJoinedRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task NotifyUserLeftRoomAsync(
            UserRoomPresenceEvent userLeftRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
