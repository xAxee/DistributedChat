namespace DistributedChat.Application.Common.Results;

public enum ApplicationErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    TooManyRequests,
    Failure,
}
