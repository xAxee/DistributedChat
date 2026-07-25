namespace DistributedChat.Infrastructure.Messaging;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";
    public const string DefaultExchangeName = "chat.events";

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    public string ExchangeName { get; set; } = DefaultExchangeName;

    public ushort PrefetchCount { get; set; } = 1;

    public static bool IsValid(RabbitMqOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.HostName)
            && options.Port is > 0 and <= 65535
            && !string.IsNullOrWhiteSpace(options.UserName)
            && options.Password is not null
            && !string.IsNullOrWhiteSpace(options.VirtualHost)
            && !string.IsNullOrWhiteSpace(options.ExchangeName)
            && options.PrefetchCount > 0;
    }
}
