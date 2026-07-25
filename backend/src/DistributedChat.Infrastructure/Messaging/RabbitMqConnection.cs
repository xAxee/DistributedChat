using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DistributedChat.Infrastructure.Messaging;

public sealed partial class RabbitMqConnection(
    IOptions<RabbitMqOptions> options,
    ILogger<RabbitMqConnection> logger
) : IDisposable
{
    private readonly object connectionLock = new();
    private readonly RabbitMqOptions options = options.Value;
    private IConnection? connection;
    private bool disposed;

    public bool IsConnected
    {
        get
        {
            lock (connectionLock)
            {
                return !disposed && connection is { IsOpen: true };
            }
        }
    }

    public IModel CreateChannel()
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        return GetOrCreateConnection().CreateModel();
    }

    public bool TryConnect()
    {
        try
        {
            using var channel = CreateChannel();

            return channel.IsOpen;
        }
        catch (Exception exception)
        {
            LogConnectionCheckFailed(logger, exception, options.HostName, options.Port, options.VirtualHost);

            return false;
        }
    }

    public void Dispose()
    {
        lock (connectionLock)
        {
            if (disposed)
            {
                return;
            }

            disposed = true;
            connection?.Dispose();
        }
    }

    private IConnection GetOrCreateConnection()
    {
        if (connection is { IsOpen: true })
        {
            return connection;
        }

        lock (connectionLock)
        {
            if (connection is { IsOpen: true })
            {
                return connection;
            }

            connection?.Dispose();

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost,
                DispatchConsumersAsync = true,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(5),
            };

            connection = factory.CreateConnection("DistributedChat");
            LogConnectionOpened(logger, options.HostName, options.Port, options.VirtualHost);

            return connection;
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "RabbitMQ connection opened to {HostName}:{Port} on virtual host {VirtualHost}.")]
    private static partial void LogConnectionOpened(
        ILogger logger,
        string hostName,
        int port,
        string virtualHost);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "RabbitMQ connection check failed for {HostName}:{Port} on virtual host {VirtualHost}.")]
    private static partial void LogConnectionCheckFailed(
        ILogger logger,
        Exception exception,
        string hostName,
        int port,
        string virtualHost);
}
