using DistributedChat.Api.Http;
using DistributedChat.Application.Users;

namespace DistributedChat.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .RequireAuthorization()
            .RequireRateLimiting(RateLimitingPolicies.Api);

        group.MapGet("/me", GetCurrentUser);

        return app;
    }

    private static async Task<IResult> GetCurrentUser(CurrentUserService currentUserService)
    {
        var result = await currentUserService.GetCurrentUserAsync();

        return result.ToResult();
    }
}
