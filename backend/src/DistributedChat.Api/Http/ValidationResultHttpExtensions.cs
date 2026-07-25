using DistributedChat.Application.Common.Results;
using FluentValidation.Results;

namespace DistributedChat.Api.Http;

public static class ValidationResultHttpExtensions
{
    public static IResult ToValidationProblemResult(this ValidationResult validationResult)
    {
        return validationResult.ToApplicationError().ToProblemResult();
    }
}