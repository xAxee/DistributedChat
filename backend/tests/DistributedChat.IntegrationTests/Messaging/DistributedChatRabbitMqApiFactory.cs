using System.Globalization;
using DistributedChat.Infrastructure.Messaging;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace DistributedChat.IntegrationTests.Messaging;

public sealed class DistributedChatRabbitMqApiFactory(
    PostgreSqlFixture postgreSqlFixture,
    RabbitMqFixture rabbitMqFixture,
    string instanceId
) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        var rabbitMqOptions = rabbitMqFixture.CreateOptions();

        Environment.SetEnvironmentVariable("ConnectionStrings__DistributedChat", postgreSqlFixture.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "DistributedChat.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "DistributedChat.Tests.Api");
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            "integration-test-signing-key-with-at-least-32-characters");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable($"{MessagingOptions.SectionName}__Transport", MessagingOptions.RabbitMqTransport);
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__HostName", rabbitMqOptions.HostName);
        Environment.SetEnvironmentVariable(
            $"{RabbitMqOptions.SectionName}__Port",
            rabbitMqOptions.Port.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__UserName", rabbitMqOptions.UserName);
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__Password", rabbitMqOptions.Password);
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__VirtualHost", rabbitMqOptions.VirtualHost);
        Environment.SetEnvironmentVariable($"{RabbitMqOptions.SectionName}__ExchangeName", rabbitMqOptions.ExchangeName);
        Environment.SetEnvironmentVariable(
            $"{RabbitMqOptions.SectionName}__PrefetchCount",
            rabbitMqOptions.PrefetchCount.ToString(CultureInfo.InvariantCulture));
        Environment.SetEnvironmentVariable($"{InstanceOptions.SectionName}__InstanceId", instanceId);

        return base.CreateHost(builder);
    }
}
