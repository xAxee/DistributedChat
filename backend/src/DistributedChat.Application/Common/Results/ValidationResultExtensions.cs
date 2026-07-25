using FluentValidation.Results;

namespace DistributedChat.Application.Common.Results;

public static class ValidationResultExtensions
{
    public static ApplicationError ToApplicationError(this ValidationResult validationResult)
    {
        var fieldErrors = validationResult.Errors
            .GroupBy(error => error.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.ErrorMessage).Distinct(StringComparer.Ordinal).ToArray(),
                StringComparer.Ordinal);

        return ApplicationError.Validation("Validation.Failed", "One or more validation errors occurred.", fieldErrors);
    }
}