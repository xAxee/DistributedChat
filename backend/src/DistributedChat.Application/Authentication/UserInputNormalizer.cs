namespace DistributedChat.Application.Authentication;

public static class UserInputNormalizer
{
    public static string NormalizeEmail(string email) => email.Trim();

    public static string NormalizeUsername(string username) => username.Trim();

    public static string NormalizeLogin(string login) => login.Trim();

    public static string ToLookupKey(string value) => value.Trim().ToUpperInvariant();
}
