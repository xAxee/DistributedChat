using DistributedChat.Api.Configuration;
using DistributedChat.Api.Status;
using Microsoft.Extensions.Options;

namespace DistributedChat.Api.Endpoints;

public static class RootEndpoints
{
    public static IEndpointRouteBuilder MapRootEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/", GetRoot)
            .Produces<ApiStatusResponse>(StatusCodes.Status200OK);

        return app;
    }

    private static IResult GetRoot(IOptions<ApplicationOptions> applicationOptions)
    {
        return Results.Ok(new ApiStatusResponse(applicationOptions.Value.Name, "API is running"));
    }
}
