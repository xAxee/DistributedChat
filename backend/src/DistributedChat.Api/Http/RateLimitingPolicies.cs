using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace DistributedChat.Api.Http;

public static class RateLimitingPolicies
{
    public const string Register = "auth-register";
    public const string Login = "auth-login";
    public const string Api = "api";

    public static IServiceCollection AddRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = WriteTooManyRequestsProblemAsync;

            options.AddPolicy(Register, context =>
                CreateFixedWindowLimiter(context, Register, permitLimit: 5));

            options.AddPolicy(Login, context =>
                CreateFixedWindowLimiter(context, Login, permitLimit: 10));

            options.AddPolicy(Api, context =>
                CreateFixedWindowLimiter(context, Api, permitLimit: 120));
        });

        return services;
    }

    private static RateLimitPartition<string> CreateFixedWindowLimiter(
        HttpContext context,
        string policyName,
        int permitLimit
    )
    {
        return RateLimitPartition.GetFixedWindowLimiter(
            GetClientPartitionKey(context, policyName),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true,
            });
    }

    private static string GetClientPartitionKey(HttpContext context, string policyName)
    {
        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            return $"{policyName}:user:{userId}";
        }

        var remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return $"{policyName}:ip:{remoteIp}";
    }

    private static async ValueTask WriteTooManyRequestsProblemAsync(
        OnRejectedContext context,
        CancellationToken _
    )
    {
        await Results.Problem(
            statusCode: StatusCodes.Status429TooManyRequests,
            title: "Too Many Requests",
            detail: "Too many requests. Please try again later."
        ).ExecuteAsync(context.HttpContext);
    }
}