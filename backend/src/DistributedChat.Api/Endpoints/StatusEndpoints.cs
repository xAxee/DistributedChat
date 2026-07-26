using System.Reflection;
using DistributedChat.Api.Http;
using DistributedChat.Api.Hubs;
using DistributedChat.Api.Status;
using DistributedChat.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace DistributedChat.Api.Endpoints;

public static class StatusEndpoints
{
    private static readonly string ApplicationVersion = GetApplicationVersion();

    public static IEndpointRouteBuilder MapStatusEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/status", GetStatus)
            .AllowAnonymous()
            .RequireRateLimiting(RateLimitingPolicies.Api)
            .Produces<ApplicationStatusResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static IResult GetStatus(
        IOptions<InstanceOptions> instanceOptions,
        ConnectionRegistry connectionRegistry,
        ApplicationStatusClock statusClock
    )
    {
        return Results.Ok(new ApplicationStatusResponse(
            instanceOptions.Value.InstanceId,
            connectionRegistry.ActiveConnectionCount,
            connectionRegistry.ConnectedUserCount,
            statusClock.UptimeSeconds,
            statusClock.StartedAt,
            ApplicationVersion));
    }

    private static string GetApplicationVersion()
    {
        var assembly = typeof(Program).Assembly;

        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "unknown";
    }
}
