using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace DistributedChat.Infrastructure.Messaging;

public static class RabbitMqTopology
{
    public static string GetInstanceQueueName(string instanceId)
    {
        return $"chat.instance.{instanceId}";
    }

    public static string GetInstanceQueueName(InstanceOptions instanceOptions)
    {
        return GetInstanceQueueName(instanceOptions.InstanceId);
    }

    public static string GetInstanceQueueName(IOptions<InstanceOptions> instanceOptions)
    {
        return GetInstanceQueueName(instanceOptions.Value);
    }

    public static void DeclareExchange(IModel channel, RabbitMqOptions options)
    {
        channel.ExchangeDeclare(
            options.ExchangeName,
            ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            arguments: null);
    }
}
