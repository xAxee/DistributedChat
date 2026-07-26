using DistributedChat.Application.Common.Results;

namespace DistributedChat.Api.Http;

public static class ResultHttpExtensions
{
    public static IResult ToResult(this Result result)
    {
        return result.IsSuccess
            ? Results.NoContent()
            : result.Error!.ToProblemResult();
    }

    public static IResult ToResult<T>(this Result<T> result)
    {
        return result.IsSuccess
            ? Results.Ok(result.Value)
            : result.Error!.ToProblemResult();
    }

    public static IResult ToCreatedResult<T>(
        this Result<T> result,
        Func<T, string> locationFactory
    )
    {
        return result.IsSuccess
            ? Results.Created(locationFactory(result.Value), result.Value)
            : result.Error!.ToProblemResult();
    }

    public static IResult ToProblemResult(this ApplicationError error)
    {
        var (statusCode, title) = GetHttpError(error.Type);

        Dictionary<string, object?> extensions = new()
        {
            ["code"] = error.Code,
        };

        if (error.FieldErrors is not null)
        {
            extensions["errors"] = error.FieldErrors;
        }

        return Results.Problem(
            statusCode: statusCode,
            title: title,
            detail: error.Message,
            extensions: extensions);
    }

    private static (int StatusCode, string Title) GetHttpError(ApplicationErrorType type) =>
        type switch
        {
            ApplicationErrorType.Validation =>
                (StatusCodes.Status400BadRequest, "Bad Request"),

            ApplicationErrorType.Unauthorized =>
                (StatusCodes.Status401Unauthorized, "Unauthorized"),

            ApplicationErrorType.Forbidden =>
                (StatusCodes.Status403Forbidden, "Forbidden"),

            ApplicationErrorType.NotFound =>
                (StatusCodes.Status404NotFound, "Not Found"),

            ApplicationErrorType.Conflict =>
                (StatusCodes.Status409Conflict, "Conflict"),

            ApplicationErrorType.TooManyRequests =>
                (StatusCodes.Status429TooManyRequests, "Too Many Requests"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal Server Error"),
        };
}
