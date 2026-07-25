using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;
using DistributedChat.Application.Rooms;
using DistributedChat.Domain.Users;

namespace DistributedChat.Application.Authentication;

public sealed class AuthService(
    IUserAccountStore userAccountStore,
    IPasswordHasher passwordHasher,
    IJwtTokenGenerator jwtTokenGenerator,
    IRoomStore roomStore,
    TimeProvider timeProvider
) : IAuthService
{
    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var email = UserInputNormalizer.NormalizeEmail(request.Email!);
        var username = UserInputNormalizer.NormalizeUsername(request.Username!);
        var normalizedEmail = UserInputNormalizer.ToLookupKey(email);
        var normalizedUsername = UserInputNormalizer.ToLookupKey(username);

        if (await userAccountStore.FindByNormalizedUsernameAsync(normalizedUsername) is not null)
        {
            return Result.Failure<AuthResponse>(AuthenticationErrors.UsernameAlreadyExists());
        }

        if (await userAccountStore.FindByNormalizedEmailAsync(normalizedEmail) is not null)
        {
            return Result.Failure<AuthResponse>(AuthenticationErrors.EmailAlreadyExists());
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = normalizedUsername,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHasher.HashPassword(request.Password!),
            CreatedAt = timeProvider.GetUtcNow(),
        };

        var created = await userAccountStore.CreateAsync(user);
        if (created.IsFailure)
        {
            return Result.Failure<AuthResponse>(created.Error!);
        }

        await roomStore.JoinAsync(GlobalRoomDefaults.Id, user.Id, timeProvider.GetUtcNow());

        return Result.Success(CreateAuthResponse(user));
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var normalizedLogin = UserInputNormalizer.ToLookupKey(UserInputNormalizer.NormalizeLogin(request.Login!));
        var user = await userAccountStore.FindByNormalizedLoginAsync(normalizedLogin);

        if (user is null || !passwordHasher.VerifyPassword(user.PasswordHash, request.Password!))
        {
            return Result.Failure<AuthResponse>(AuthenticationErrors.InvalidCredentials());
        }

        return Result.Success(CreateAuthResponse(user));
    }

    private AuthResponse CreateAuthResponse(User user)
    {
        var token = jwtTokenGenerator.GenerateToken(user);

        return new AuthResponse(
            token.AccessToken,
            token.ExpiresAt,
            new CurrentUserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }
}
