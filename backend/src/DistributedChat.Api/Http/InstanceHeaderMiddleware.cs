using DistributedChat.Infrastructure.Messaging;
using Microsoft.Extensions.Options;

namespace DistributedChat.Api.Http;

public sealed class InstanceHeaderMiddleware(
    RequestDelegate next,
    IOptions<InstanceOptions> instanceOptions)
{
    public const string HeaderName = "X-DistributedChat-Instance";

    private readonly string instanceId =
        instanceOptions.Value.InstanceId;

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers[HeaderName] = instanceId;

        return next(context);
    }
}
