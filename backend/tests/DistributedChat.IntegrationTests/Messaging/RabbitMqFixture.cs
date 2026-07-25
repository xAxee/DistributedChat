using DistributedChat.Infrastructure.Messaging;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace DistributedChat.IntegrationTests.Messaging;

public sealed class RabbitMqFixture : IAsyncLifetime
{
    private const int RabbitMqPort = 5672;
    private const string RabbitMqImage = "rabbitmq:4.1-management-alpine";
    private const string UserName = "distributed_chat";
    private const string Password = "distributed_chat_test_password";
    private const string VirtualHost = "distributed_chat_tests";

    private readonly IContainer container = new ContainerBuilder(RabbitMqImage)
        .WithEnvironment("RABBITMQ_DEFAULT_USER", UserName)
        .WithEnvironment("RABBITMQ_DEFAULT_PASS", Password)
        .WithEnvironment("RABBITMQ_DEFAULT_VHOST", VirtualHost)
        .WithPortBinding(RabbitMqPort, assignRandomHostPort: true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(RabbitMqPort))
        .Build();

    public string HostName => container.Hostname;

    public int Port => container.GetMappedPublicPort(RabbitMqPort);

    public Task InitializeAsync()
    {
        return container.StartAsync();
    }

    public Task DisposeAsync()
    {
        return container.DisposeAsync().AsTask();
    }

    public RabbitMqOptions CreateOptions()
    {
        return new RabbitMqOptions
        {
            HostName = HostName,
            Port = Port,
            UserName = UserName,
            Password = Password,
            VirtualHost = VirtualHost,
            ExchangeName = RabbitMqOptions.DefaultExchangeName,
            PrefetchCount = 2,
        };
    }

    public IConnection CreateConnection()
    {
        var options = CreateOptions();
        var factory = new ConnectionFactory
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            DispatchConsumersAsync = true,
        };

        return factory.CreateConnection("DistributedChat.IntegrationTests");
    }

    public async Task WaitForQueueAsync(
        string queueName,
        CancellationToken cancellationToken = default
    )
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var connection = CreateConnection();
                using var channel = connection.CreateModel();
                channel.QueueDeclarePassive(queueName);

                return;
            }
            catch (OperationInterruptedException)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
            }
        }

        throw new TimeoutException($"Timed out waiting for RabbitMQ queue '{queueName}'.");
    }

    public async Task WaitForQueueReadyMessagesAsync(
        string queueName,
        uint expectedReadyMessages,
        CancellationToken cancellationToken = default
    )
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(15);

        while (DateTimeOffset.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using (var connection = CreateConnection())
            using (var channel = connection.CreateModel())
            {
                var declaration = channel.QueueDeclarePassive(queueName);
                if (declaration.MessageCount == expectedReadyMessages)
                {
                    return;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }

        throw new TimeoutException(
            $"Timed out waiting for RabbitMQ queue '{queueName}' to contain {expectedReadyMessages} ready messages.");
    }

    public void DeleteQueue(string queueName)
    {
        using var connection = CreateConnection();
        using var channel = connection.CreateModel();
        channel.QueueDelete(queueName, ifUnused: false, ifEmpty: false);
    }
}
