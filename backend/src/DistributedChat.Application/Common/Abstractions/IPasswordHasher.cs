namespace DistributedChat.Application.Common.Abstractions;

public interface IPasswordHasher
{
    string HashPassword(string password);

    bool VerifyPassword(string passwordHash, string password);
}
