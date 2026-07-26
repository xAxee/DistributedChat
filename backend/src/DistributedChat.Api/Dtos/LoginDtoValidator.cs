using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(request => request.Login)
            .NotEmpty()
            .WithMessage("Login is required.");

        RuleFor(request => request.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}
