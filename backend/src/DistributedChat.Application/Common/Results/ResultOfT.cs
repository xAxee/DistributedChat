namespace DistributedChat.Application.Common.Results;

public sealed class Result<T> : Result
{
    private readonly T? value;

    internal Result(T value)
        : base(true, null)
    {
        this.value = value;
    }

    internal Result(ApplicationError error)
        : base(false, error)
    {
    }

    public T Value =>
        IsSuccess
            ? value!
            : throw new InvalidOperationException("A failed result does not contain a value.");

    internal static Result<T> CreateSuccess(T value) => new(value);

    internal static Result<T> CreateFailure(ApplicationError error) => new(error);
}
