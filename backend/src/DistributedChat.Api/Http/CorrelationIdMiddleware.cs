namespace DistributedChat.Api.Http;

using Serilog.Context;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = TryReadCorrelationId(context) ?? Guid.NewGuid();

        context.Items[CorrelationContext.CorrelationIdItemName] = correlationId;
        context.Response.Headers[CorrelationContext.CorrelationIdHeaderName] = correlationId.ToString("D");

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("TraceId", context.TraceIdentifier))
        {
            await next(context);
        }
    }

    private static Guid? TryReadCorrelationId(HttpContext context)
    {
        var value = context.Request.Headers[CorrelationContext.CorrelationIdHeaderName];

        return Guid.TryParse(value, out var correlationId)
            ? correlationId
            : null;
    }
}
