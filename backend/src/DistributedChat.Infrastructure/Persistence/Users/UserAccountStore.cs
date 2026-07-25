using DistributedChat.Application.Authentication;
using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Results;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DistributedChat.Infrastructure.Persistence.Users;

public sealed class UserAccountStore(DistributedChatDbContext dbContext) : IUserAccountStore
{
    private const string UniqueUsernameIndexName = "ux_users_normalized_username";
    private const string UniqueEmailIndexName = "ux_users_normalized_email";

    public Task<User?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == userId, cancellationToken);
    }

    public Task<User?> FindByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedEmail == normalizedEmail,
                cancellationToken);
    }

    public Task<User?> FindByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken);
    }

    public Task<User?> FindByNormalizedLoginAsync(
        string normalizedLogin,
        CancellationToken cancellationToken = default
    )
    {
        return normalizedLogin.Contains('@', StringComparison.Ordinal)
            ? FindByNormalizedEmailAsync(normalizedLogin, cancellationToken)
            : FindByNormalizedUsernameAsync(normalizedLogin, cancellationToken);
    }

    public async Task<Result> CreateAsync(User user, CancellationToken cancellationToken = default)
    {
        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (TryMapUniqueViolation(exception, out var error))
        {
            dbContext.Entry(user).State = EntityState.Detached;

            return Result.Failure(error);
        }

        return Result.Success();
    }

    private static bool TryMapUniqueViolation(DbUpdateException exception, out ApplicationError error)
    {
        if (exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation)
        {
            error = postgresException.ConstraintName switch
            {
                UniqueUsernameIndexName => AuthenticationErrors.UsernameAlreadyExists(),
                UniqueEmailIndexName => AuthenticationErrors.EmailAlreadyExists(),
                _ => ApplicationError.Conflict("Persistence.UniqueConstraint", "A unique constraint was violated."),
            };

            return true;
        }

        error = ApplicationError.Failure("Persistence.SaveFailed", "Could not save changes.");
        return false;
    }
}
