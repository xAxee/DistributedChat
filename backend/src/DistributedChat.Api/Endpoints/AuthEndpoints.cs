using DistributedChat.Api.Dtos;
using DistributedChat.Api.Http;
using DistributedChat.Application.Authentication;
using FluentValidation;

namespace DistributedChat.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").AllowAnonymous();

        group.MapPost("/register", Register)
            .RequireRateLimiting(RateLimitingPolicies.Register);

        group.MapPost("/login", Login)
            .RequireRateLimiting(RateLimitingPolicies.Login);

        return app;
    }

    private static async Task<IResult> Register(
        RegisterDto dto,
        IValidator<RegisterDto> validator,
        IAuthService authService
    )
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblemResult();
        }

        var request = new RegisterRequest(
            dto.Email!,
            dto.Username!.Trim(),
            dto.Password!);

        var result = await authService.RegisterAsync(request);

        return result.ToResult();
    }

    private static async Task<IResult> Login(
        LoginDto dto,
        IValidator<LoginDto> validator,
        IAuthService authService
    )
    {
        var validation = await validator.ValidateAsync(dto);
        if (!validation.IsValid)
        {
            return validation.ToValidationProblemResult();
        }

        var request = new LoginRequest(dto.Login!.Trim(), dto.Password!);
        var result = await authService.LoginAsync(request);

        return result.ToResult();
    }
}