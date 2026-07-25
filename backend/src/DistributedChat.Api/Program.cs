using DistributedChat.Api.Extensions;
using DistributedChat.Api.Http;
using DistributedChat.Application;
using DistributedChat.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDistributedChatSerilog();

builder.Services
    .AddApiOptions(builder.Configuration)
    .AddApiHttpContext()
    .AddApiEndpoints()
    .AddApiHealthChecks()
    .AddApiSignalR()
    .AddRateLimiting()
    .AddApiSwagger()
    .AddApiProblemDetails()
    .AddJwtAuthentication(builder.Configuration)
    .AddAuthorization()
    .AddApplication()
    .AddInfrastructure(builder.Configuration)
    .AddApiMessaging(builder.Configuration);

var app = builder.Build();

if (await app.ApplyDatabaseMigrationsIfRequestedAsync(args))
{
    return;
}

app.UseApiPipeline();
app.MapApiEndpoints();

app.Run();

public sealed partial class Program;
