using System.Globalization;
using DistributedChat.Infrastructure.Messaging;
using Serilog;

namespace DistributedChat.Api.Extensions;

public static class HostBuilderExtensions
{
    public static ConfigureHostBuilder UseDistributedChatSerilog(
        this ConfigureHostBuilder host
    )
    {
        host.UseSerilog((ctx, _, logger) =>
        {
            var instanceId = ctx.Configuration[$"{InstanceOptions.SectionName}:InstanceId"]
                ?? Environment.MachineName;

            logger
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("InstanceId", instanceId)
                .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture);
        });

        return host;
    }
}