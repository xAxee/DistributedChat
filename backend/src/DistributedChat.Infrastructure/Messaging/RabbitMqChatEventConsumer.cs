using System.Text.Json;
using DistributedChat.Application.Messages;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Serilog.Context;

namespace DistributedChat.Infrastructure.Messaging;

public sealed partial class RabbitMqChatEventConsumer(
    RabbitMqConnection connection,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqOptions> rabbitMqOptions,
    IOptions<InstanceOptions> instanceOptions,
    ILogger<RabbitMqChatEventConsumer> logger
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RabbitMqOptions rabbitMqOptions = rabbitMqOptions.Value;
    private readonly string consumerId = instanceOptions.Value.InstanceId;
    private readonly string queueName = RabbitMqTopology.GetInstanceQueueName(instanceOptions.Value);
    private IModel? channel;
    private CancellationToken stoppingToken;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.stoppingToken = stoppingToken;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                StartConsumer();
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                LogConsumerFailure(logger, exception, consumerId);
                CloseChannel();
                await DelayBeforeRetryAsync(stoppingToken);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        CloseChannel();
        await base.StopAsync(cancellationToken);
    }

    public override void Dispose()
    {
        CloseChannel();
        base.Dispose();
    }

    private void StartConsumer()
    {
        CloseChannel();

        channel = connection.CreateChannel();
        RabbitMqTopology.DeclareExchange(channel, rabbitMqOptions);
        channel.QueueDeclare(
            queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
        channel.QueueBind(
            queueName,
            rabbitMqOptions.ExchangeName,
            routingKey: string.Empty,
            arguments: null);
        channel.BasicQos(
            prefetchSize: 0,
            prefetchCount: rabbitMqOptions.PrefetchCount,
            global: false);

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += HandleMessageAsync;
        channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        LogConsumerStarted(logger, rabbitMqOptions.ExchangeName, queueName, rabbitMqOptions.PrefetchCount);
    }

    private async Task HandleMessageAsync(object sender, BasicDeliverEventArgs eventArgs)
    {
        try
        {
            var envelope = Deserialize(
                eventArgs.BasicProperties.Type,
                eventArgs.BasicProperties.MessageId,
                eventArgs.Body);
            using var eventScope = LogContext.PushProperty("EventId", envelope.EventId);
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var processor = scope.ServiceProvider.GetRequiredService<ChatEventProcessor>();

            var result = await processor.ProcessAsync(consumerId, envelope, stoppingToken);
            Ack(eventArgs.DeliveryTag);

            if (result == ChatEventProcessingResult.Duplicate)
            {
                LogDuplicateEventSkipped(logger, envelope.EventId, consumerId);
            }
        }
        catch (InvalidChatEventException exception)
        {
            LogInvalidEventRejected(logger, consumerId, exception.GetType().Name);
            Reject(eventArgs.DeliveryTag);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Nack(eventArgs.DeliveryTag);
        }
        catch (Exception exception)
        {
            LogTransientEventFailure(logger, exception, consumerId);
            Nack(eventArgs.DeliveryTag);
        }
    }

    private static ChatEventEnvelope Deserialize(
        string? eventType,
        string? messageId,
        ReadOnlyMemory<byte> body
    )
    {
        return eventType switch
        {
            ChatEventTypes.MessageCreated => DeserializeMessageCreated(messageId, body),
            ChatEventTypes.UserJoinedRoom => DeserializeUserJoinedRoom(messageId, body),
            ChatEventTypes.UserLeftRoom => DeserializeUserLeftRoom(messageId, body),
            _ => throw new InvalidChatEventException("Chat event type is missing or unsupported."),
        };
    }

    private static ChatEventEnvelope DeserializeMessageCreated(string? messageId, ReadOnlyMemory<byte> body)
    {
        var eventId = ParseEventId(messageId);
        var messageCreated = DeserializePayload<ChatMessageCreated>(body, "Chat message event payload is empty.");
        Validate(messageCreated, eventId);

        return new ChatEventEnvelope(
            eventId,
            (notifier, cancellationToken) => notifier.NotifyMessageReceivedAsync(messageCreated, cancellationToken));
    }

    private static ChatEventEnvelope DeserializeUserJoinedRoom(string? messageId, ReadOnlyMemory<byte> body)
    {
        var eventId = ParseEventId(messageId);
        var userJoinedRoom = DeserializePayload<UserRoomPresenceEvent>(body, "User joined room event payload is empty.");
        Validate(userJoinedRoom, eventId);

        return new ChatEventEnvelope(
            eventId,
            (notifier, cancellationToken) => notifier.NotifyUserJoinedRoomAsync(userJoinedRoom, cancellationToken));
    }

    private static ChatEventEnvelope DeserializeUserLeftRoom(string? messageId, ReadOnlyMemory<byte> body)
    {
        var eventId = ParseEventId(messageId);
        var userLeftRoom = DeserializePayload<UserRoomPresenceEvent>(body, "User left room event payload is empty.");
        Validate(userLeftRoom, eventId);

        return new ChatEventEnvelope(
            eventId,
            (notifier, cancellationToken) => notifier.NotifyUserLeftRoomAsync(userLeftRoom, cancellationToken));
    }

    private static Guid ParseEventId(string? messageId)
    {
        if (!Guid.TryParse(messageId, out var eventId) || eventId == Guid.Empty)
        {
            throw new InvalidChatEventException("Chat event message id is missing or invalid.");
        }

        return eventId;
    }

    private static TPayload DeserializePayload<TPayload>(ReadOnlyMemory<byte> body, string emptyMessage)
    {
        try
        {
            return JsonSerializer.Deserialize<TPayload>(body.Span, JsonOptions)
                ?? throw new InvalidChatEventException(emptyMessage);
        }
        catch (JsonException exception)
        {
            throw new InvalidChatEventException("Chat event payload is not valid JSON.", exception);
        }
        catch (ArgumentException exception)
        {
            throw new InvalidChatEventException("Chat event payload is invalid.", exception);
        }
    }

    private static void Validate(ChatMessageCreated messageCreated, Guid eventId)
    {
        if (messageCreated.EventId == Guid.Empty
            || messageCreated.EventId != eventId
            || messageCreated.MessageId == Guid.Empty
            || messageCreated.RoomId == Guid.Empty
            || messageCreated.SenderUserId == Guid.Empty
            || string.IsNullOrWhiteSpace(messageCreated.SenderUsername)
            || string.IsNullOrWhiteSpace(messageCreated.Content))
        {
            throw new InvalidChatEventException("Chat message event payload is invalid.");
        }
    }

    private static void Validate(UserRoomPresenceEvent presenceEvent, Guid eventId)
    {
        if (presenceEvent.EventId == Guid.Empty
            || presenceEvent.EventId != eventId
            || presenceEvent.RoomId == Guid.Empty
            || presenceEvent.UserId == Guid.Empty
            || string.IsNullOrWhiteSpace(presenceEvent.ConnectionId)
            || string.IsNullOrWhiteSpace(presenceEvent.InstanceId))
        {
            throw new InvalidChatEventException("User room presence event payload is invalid.");
        }
    }

    private static async Task DelayBeforeRetryAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void Ack(ulong deliveryTag)
    {
        channel?.BasicAck(deliveryTag, multiple: false);
    }

    private void Reject(ulong deliveryTag)
    {
        channel?.BasicReject(deliveryTag, requeue: false);
    }

    private void Nack(ulong deliveryTag)
    {
        channel?.BasicNack(deliveryTag, multiple: false, requeue: true);
    }

    private void CloseChannel()
    {
        var channelToClose = channel;
        channel = null;

        if (channelToClose is null)
        {
            return;
        }

        try
        {
            if (channelToClose.IsOpen)
            {
                channelToClose.Close();
            }
        }
        catch (Exception exception)
        {
            LogChannelCloseFailure(logger, exception, consumerId);
        }

        try
        {
            channelToClose.Dispose();
        }
        catch (Exception exception)
        {
            LogChannelDisposeFailure(logger, exception, consumerId);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "RabbitMQ chat event consumer started for exchange {ExchangeName}, queue {QueueName}, prefetch {PrefetchCount}.")]
    private static partial void LogConsumerStarted(
        ILogger logger,
        string exchangeName,
        string queueName,
        ushort prefetchCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Rejected invalid RabbitMQ chat event for consumer {ConsumerId} without requeue. Error type: {ErrorType}.")]
    private static partial void LogInvalidEventRejected(
        ILogger logger,
        string consumerId,
        string errorType);

    [LoggerMessage(
        EventId = 3,
        Level = LogLevel.Warning,
        Message = "RabbitMQ chat event processing failed for consumer {ConsumerId}; message will be requeued.")]
    private static partial void LogTransientEventFailure(
        ILogger logger,
        Exception exception,
        string consumerId);

    [LoggerMessage(
        EventId = 4,
        Level = LogLevel.Error,
        Message = "RabbitMQ chat event consumer failed for consumer {ConsumerId}; retrying.")]
    private static partial void LogConsumerFailure(
        ILogger logger,
        Exception exception,
        string consumerId);

    [LoggerMessage(
        EventId = 5,
        Level = LogLevel.Debug,
        Message = "Skipped duplicate RabbitMQ chat event {EventId} for consumer {ConsumerId}.")]
    private static partial void LogDuplicateEventSkipped(
        ILogger logger,
        Guid eventId,
        string consumerId);

    [LoggerMessage(
        EventId = 6,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel close failed for consumer {ConsumerId}.")]
    private static partial void LogChannelCloseFailure(
        ILogger logger,
        Exception exception,
        string consumerId);

    [LoggerMessage(
        EventId = 7,
        Level = LogLevel.Debug,
        Message = "RabbitMQ channel dispose failed for consumer {ConsumerId}.")]
    private static partial void LogChannelDisposeFailure(
        ILogger logger,
        Exception exception,
        string consumerId);
}