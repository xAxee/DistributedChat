using DistributedChat.Domain.Users;
using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(request => request.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Email is required.")
            .EmailAddress()
                .WithMessage("Email must be valid.")
            .Must(value => value!.Trim().Length <= User.MaximumEmailLength)
                .WithMessage($"Email must be {User.MaximumEmailLength} characters or fewer.");

        RuleFor(request => request.Username)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Username is required.")
            .Must(value => value!.Trim().Length >= User.MinimumUsernameLength)
                .WithMessage($"Username must be at least {User.MinimumUsernameLength} characters.")
            .Must(value => value!.Trim().Length <= User.MaximumUsernameLength)
                .WithMessage($"Username must be {User.MaximumUsernameLength} characters or fewer.")
            .Must(value => value!.Trim().All(char.IsLetterOrDigit))
                .WithMessage("Username can contain only letters and digits.");

        RuleFor(request => request.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Password is required.")
            .Length(8, 128)
                .WithMessage("Password must be between 8 and 128 characters.");
    }
}
