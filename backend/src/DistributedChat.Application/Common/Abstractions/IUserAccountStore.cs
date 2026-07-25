using DistributedChat.Application.Common.Results;
using DistributedChat.Domain.Users;

namespace DistributedChat.Application.Common.Abstractions;

public interface IUserAccountStore
{
    Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<User?> FindByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> FindByNormalizedUsernameAsync(string normalizedUsername, CancellationToken cancellationToken = default);

    Task<User?> FindByNormalizedLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);

    Task<Result> CreateAsync(User user, CancellationToken cancellationToken = default);
}
