using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Authentication;

public static class AuthenticationErrors
{
    public static ApplicationError UsernameAlreadyExists() =>
        ApplicationError.Conflict("Users.UsernameAlreadyExists", "Username is already in use.");

    public static ApplicationError EmailAlreadyExists() =>
        ApplicationError.Conflict("Users.EmailAlreadyExists", "Email is already in use.");

    public static ApplicationError InvalidCredentials() =>
        ApplicationError.Unauthorized("Auth.InvalidCredentials", "Invalid login or password.");
}
