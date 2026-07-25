namespace DistributedChat.Application.Common.Results;

public sealed record ApplicationError(
    ApplicationErrorType Type,
    string Code,
    string Message,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null
)
{
    public static ApplicationError Validation(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null
    ) => new(ApplicationErrorType.Validation, code, message, fieldErrors);

    public static ApplicationError Unauthorized(string code, string message) =>
        new(ApplicationErrorType.Unauthorized, code, message);

    public static ApplicationError Forbidden(string code, string message) =>
        new(ApplicationErrorType.Forbidden, code, message);

    public static ApplicationError NotFound(string code, string message) =>
        new(ApplicationErrorType.NotFound, code, message);

    public static ApplicationError Conflict(string code, string message) =>
        new(ApplicationErrorType.Conflict, code, message);

    public static ApplicationError TooManyRequests(string code, string message) =>
        new(ApplicationErrorType.TooManyRequests, code, message);

    public static ApplicationError Failure(string code, string message) =>
        new(ApplicationErrorType.Failure, code, message);
}
