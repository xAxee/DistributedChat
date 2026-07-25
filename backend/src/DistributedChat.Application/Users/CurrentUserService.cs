using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Users;

public sealed class CurrentUserService(ICurrentUser currentUser, IUserAccountStore userAccountStore)
{
    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return Result.Failure<CurrentUserDto>(
                ApplicationError.Unauthorized("Auth.Unauthenticated", "Authentication is required."));
        }

        var user = await userAccountStore.FindByIdAsync(currentUser.UserId.Value);
        if (user is null)
        {
            return Result.Failure<CurrentUserDto>(
                ApplicationError.NotFound("Users.CurrentUserNotFound", "Current user was not found."));
        }

        return Result.Success(
            new CurrentUserDto(user.Id, user.Username, user.Email, user.CreatedAt));
    }
}
