using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Presence;
using DistributedChat.Infrastructure.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DistributedChat.Infrastructure.Persistence.Users;

public sealed partial class UserPresenceBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    IOptions<InstanceOptions> instanceOptions,
    TimeProvider timeProvider,
    ILogger<UserPresenceBackgroundService> logger
) : BackgroundService
{
    private readonly string instanceId = instanceOptions.Value.InstanceId;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await ClearInstanceStateAsync(stoppingToken);

        using var timer = new PeriodicTimer(UserPresenceDefaults.HeartbeatInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await HeartbeatAsync(stoppingToken);
        }
    }

    private async Task ClearInstanceStateAsync(CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var presenceStore = scope.ServiceProvider.GetRequiredService<IUserPresenceStore>();

        await presenceStore.ClearInstanceAsync(instanceId, cancellationToken);
        LogInstancePresenceCleared(logger, instanceId);
    }

    private async Task HeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var presenceStore = scope.ServiceProvider.GetRequiredService<IUserPresenceStore>();

            await presenceStore.HeartbeatAsync(instanceId, timeProvider.GetUtcNow(), cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            LogHeartbeatFailure(logger, exception, instanceId);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Cleared stale user presence state for instance {InstanceId}.")]
    private static partial void LogInstancePresenceCleared(ILogger logger, string instanceId);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "User presence heartbeat failed for instance {InstanceId}.")]
    private static partial void LogHeartbeatFailure(ILogger logger, Exception exception, string instanceId);
}
