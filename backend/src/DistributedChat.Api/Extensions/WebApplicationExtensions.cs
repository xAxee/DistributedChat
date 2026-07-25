using DistributedChat.Api.Endpoints;
using DistributedChat.Api.Http;
using DistributedChat.Api.Hubs;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace DistributedChat.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseMiddleware<InstanceHeaderMiddleware>();
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRateLimiter();
        app.UseAuthorization();

        return app;
    }

    public static WebApplication MapApiEndpoints(this WebApplication app)
    {
        app.MapHealthChecks(
            "/health/live",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("live", StringComparer.OrdinalIgnoreCase),
            });
        app.MapHealthChecks(
            "/health/ready",
            new HealthCheckOptions
            {
                Predicate = registration => registration.Tags.Contains("ready", StringComparer.OrdinalIgnoreCase),
            });
        app.MapRootEndpoints();
        app.MapAuthEndpoints();
        app.MapUserEndpoints();
        app.MapRoomEndpoints();
        app.MapStatusEndpoints();
        app.MapHub<ChatHub>("/hubs/chat").RequireAuthorization();

        return app;
    }
}