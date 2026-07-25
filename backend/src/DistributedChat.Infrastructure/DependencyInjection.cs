using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using DistributedChat.Application.Rooms;
using DistributedChat.Infrastructure.Authentication;
using DistributedChat.Infrastructure.Messaging;
using DistributedChat.Infrastructure.Persistence;
using DistributedChat.Infrastructure.Persistence.Messages;
using DistributedChat.Infrastructure.Persistence.Rooms;
using DistributedChat.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedChat.Infrastructure;

public static class DependencyInjection
{
    public const string DistributedChatConnectionStringName = "DistributedChat";

    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        var connectionString = configuration.GetConnectionString(DistributedChatConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{DistributedChatConnectionStringName}' is required. "
                    + $"Set it with the 'ConnectionStrings__{DistributedChatConnectionStringName}' environment variable."
            );
        }

        services.AddDbContext<DistributedChatDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(DistributedChatDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure();
                }
            );
        });

        var messagingOptions = configuration
            .GetSection(MessagingOptions.SectionName)
            .Get<MessagingOptions>() ?? new MessagingOptions();

        services
            .AddOptions<MessagingOptions>()
            .Bind(configuration.GetSection(MessagingOptions.SectionName))
            .Validate(options => options.IsSupportedTransport(), "Messaging transport is not supported.")
            .ValidateOnStart();

        services
            .AddOptions<InstanceOptions>()
            .Bind(configuration.GetSection(InstanceOptions.SectionName))
            .Validate(InstanceOptions.IsValid, "Instance configuration is invalid.")
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrWhiteSpace(options.SigningKey), "JWT signing key is required.")
            .ValidateOnStart();

        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IUserAccountStore, UserAccountStore>();
        services.AddScoped<IUserPresenceStore, UserPresenceStore>();
        services.AddScoped<IRoomStore, RoomStore>();
        services.AddScoped<IMessageStore, MessageStore>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddHostedService<UserPresenceBackgroundService>();

        if (messagingOptions.IsRabbitMqTransport())
        {
            services
                .AddOptions<RabbitMqOptions>()
                .Bind(configuration.GetSection(RabbitMqOptions.SectionName))
                .Validate(RabbitMqOptions.IsValid, "RabbitMQ configuration is invalid.")
                .ValidateOnStart();

            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<IChatEventPublisher, RabbitMqChatEventPublisher>();
            services.AddScoped<ChatEventProcessor>();
            services.AddHostedService<RabbitMqChatEventConsumer>();
        }

        return services;
    }
}
