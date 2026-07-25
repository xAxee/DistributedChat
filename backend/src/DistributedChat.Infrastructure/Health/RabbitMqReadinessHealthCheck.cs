using DistributedChat.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace DistributedChat.Infrastructure.Health;

public sealed class RabbitMqReadinessHealthCheck(
    IServiceProvider serviceProvider,
    IOptions<MessagingOptions> messagingOptions
) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        if (!messagingOptions.Value.IsRabbitMqTransport())
        {
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ transport is disabled."));
        }

        var connection = serviceProvider.GetService<RabbitMqConnection>();
        if (connection is null)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ connection is not registered."));
        }

        return Task.FromResult(connection.TryConnect()
            ? HealthCheckResult.Healthy("RabbitMQ is reachable.")
            : HealthCheckResult.Unhealthy("RabbitMQ is not reachable."));
    }
}
