using DistributedChat.Api.Configuration;
using DistributedChat.Api.Dtos;
using DistributedChat.Api.Http;
using DistributedChat.Api.Hubs;
using DistributedChat.Api.Status;
using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Infrastructure.Health;
using DistributedChat.Infrastructure.Messaging;
using FluentValidation;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;

namespace DistributedChat.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApiOptions(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services
            .AddOptions<ApplicationOptions>()
            .Bind(configuration.GetSection(ApplicationOptions.SectionName));

        return services;
    }

    public static IServiceCollection AddApiHttpContext(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, CurrentUser>();

        return services;
    }

    public static IServiceCollection AddApiEndpoints(this IServiceCollection services)
    {
        services.AddScoped<IValidator<RegisterDto>, RegisterDtoValidator>();
        services.AddScoped<IValidator<LoginDto>, LoginDtoValidator>();
        services.AddScoped<IValidator<CreateRoomDto>, CreateRoomDtoValidator>();
        services.AddScoped<IValidator<UpdateRoomDto>, UpdateRoomDtoValidator>();
        services.AddScoped<IValidator<ChangeRoomPasswordDto>, ChangeRoomPasswordDtoValidator>();

        return services;
    }

    public static IServiceCollection AddApiHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
            .AddCheck<DatabaseReadinessHealthCheck>("postgresql", tags: ["ready"])
            .AddCheck<RabbitMqReadinessHealthCheck>("rabbitmq", tags: ["ready"]);

        return services;
    }

    public static IServiceCollection AddApiSignalR(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<ConnectionRegistry>();
        services.AddSingleton<HubCurrentUserContext>();
        services.AddSingleton<ChatHubUserResolver>();
        services.AddSingleton<ApplicationStatusClock>();
        services.AddSingleton<LocalSignalRSendMessageRateLimiter>();
        services.AddScoped<ChatConnectionLifecycleService>();
        services.AddScoped<ChatRoomSubscriptionService>();
        services.AddSingleton<IChatClientNotifier, SignalRChatClientNotifier>();

        return services;
    }

    public static IServiceCollection AddApiSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(
                "v1",
                new OpenApiInfo
                {
                    Title = "DistributedChat API",
                    Version = "v1",
                });

            options.AddSecurityDefinition(
                "Bearer",
                new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'.",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                });

            options.AddSecurityRequirement(_ =>
                new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", null, null)] = [],
                });
        });

        return services;
    }

    public static IServiceCollection AddApiProblemDetails(this IServiceCollection services)
    {
        services.AddProblemDetails();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        return services;
    }

    public static IServiceCollection AddApiMessaging(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var messagingOptions = configuration
            .GetSection(MessagingOptions.SectionName)
            .Get<MessagingOptions>() ?? new MessagingOptions();

        if (string.Equals(
            messagingOptions.Transport,
            MessagingOptions.InMemoryTransport,
            StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IChatEventPublisher, InProcessChatEventPublisher>();
        }

        return services;
    }
}
