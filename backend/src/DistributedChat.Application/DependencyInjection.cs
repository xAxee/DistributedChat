using DistributedChat.Application.Authentication;
using DistributedChat.Application.Messages;
using DistributedChat.Application.Rooms;
using DistributedChat.Application.Users;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DistributedChat.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IValidator<SendMessageRequest>, SendMessageRequestValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IMessageService, MessageService>();

        return services;
    }
}
