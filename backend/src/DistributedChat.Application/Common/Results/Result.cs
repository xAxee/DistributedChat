namespace DistributedChat.Application.Common.Results;

public class Result
{
    protected Result(bool isSuccess, ApplicationError? error)
    {
        if (isSuccess && error is not null)
        {
            throw new ArgumentException("A successful result cannot contain an error.", nameof(error));
        }

        if (!isSuccess && error is null)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public ApplicationError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, error);
    }

    public static Result<T> Failure<T>(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return Result<T>.CreateFailure(error);
    }
}
