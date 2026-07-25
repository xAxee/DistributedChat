using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Domain.Users;
using Microsoft.AspNetCore.Identity;

namespace DistributedChat.Infrastructure.Authentication;

public sealed class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> passwordHasher = new();

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        return passwordHasher.HashPassword(user: null!, password);
    }

    public bool VerifyPassword(string passwordHash, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var result = passwordHasher.VerifyHashedPassword(user: null!, passwordHash, password);

        return result is PasswordVerificationResult.Success or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
