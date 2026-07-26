using System.Text.Json;
using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using Serilog.Context;

namespace DistributedChat.Infrastructure.Messaging;

public sealed class RabbitMqChatEventPublisher(
    RabbitMqConnection connection,
    IOptions<RabbitMqOptions> options
) : IChatEventPublisher, IDisposable
{
    private const string JsonContentType = "application/json";
    private const string Utf8ContentEncoding = "utf-8";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim publishLock = new(1, 1);
    private readonly RabbitMqOptions options = options.Value;
    private IModel? channel;
    private bool disposed;

    public async Task PublishMessageCreatedAsync(
        ChatMessageCreated messageCreated,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(messageCreated);

        using var eventScope = LogContext.PushProperty("EventId", messageCreated.EventId);
        using var messageScope = LogContext.PushProperty("MessageId", messageCreated.MessageId);
        using var roomScope = LogContext.PushProperty("RoomId", messageCreated.RoomId);
        using var userScope = LogContext.PushProperty("UserId", messageCreated.SenderUserId);

        await PublishAsync(ChatEventTypes.MessageCreated, messageCreated.EventId, messageCreated, cancellationToken);
    }

    public async Task PublishUserJoinedRoomAsync(
        UserRoomPresenceEvent userJoinedRoom,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(userJoinedRoom);

        using var eventScope = LogContext.PushProperty("EventId", userJoinedRoom.EventId);
        using var roomScope = LogContext.PushProperty("RoomId", userJoinedRoom.RoomId);
        using var userScope = LogContext.PushProperty("UserId", userJoinedRoom.UserId);

        await PublishAsync(ChatEventTypes.UserJoinedRoom, userJoinedRoom.EventId, userJoinedRoom, cancellationToken);
    }

    public async Task PublishUserLeftRoomAsync(
        UserRoomPresenceEvent userLeftRoom,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(userLeftRoom);

        using var eventScope = LogContext.PushProperty("EventId", userLeftRoom.EventId);
        using var roomScope = LogContext.PushProperty("RoomId", userLeftRoom.RoomId);
        using var userScope = LogContext.PushProperty("UserId", userLeftRoom.UserId);

        await PublishAsync(ChatEventTypes.UserLeftRoom, userLeftRoom.EventId, userLeftRoom, cancellationToken);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        CloseChannel();
        publishLock.Dispose();
    }

    private async Task PublishAsync<TEvent>(
        string eventType,
        Guid eventId,
        TEvent payload,
        CancellationToken cancellationToken
    )
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        await publishLock.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var publishChannel = GetOrCreateChannel();
            var properties = publishChannel.CreateBasicProperties();
            properties.ContentType = JsonContentType;
            properties.ContentEncoding = Utf8ContentEncoding;
            properties.MessageId = eventId.ToString("D");
            properties.Type = eventType;
            properties.DeliveryMode = 2;

            var body = JsonSerializer.SerializeToUtf8Bytes(payload, JsonOptions);

            publishChannel.BasicPublish(
                options.ExchangeName,
                routingKey: string.Empty,
                mandatory: false,
                basicProperties: properties,
                body: body);
            publishChannel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));
        }
        catch
        {
            CloseChannel();
            throw;
        }
        finally
        {
            publishLock.Release();
        }
    }

    private IModel GetOrCreateChannel()
    {
        if (channel is { IsOpen: true })
        {
            return channel;
        }

        CloseChannel();
        channel = connection.CreateChannel();
        RabbitMqTopology.DeclareExchange(channel, options);
        channel.ConfirmSelect();

        return channel;
    }

    private void CloseChannel()
    {
        channel?.Dispose();
        channel = null;
    }
}
