using DistributedChat.Domain.Rooms;
using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class CreateRoomDtoValidator : AbstractValidator<CreateRoomDto>
{
    public CreateRoomDtoValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Room name is required.")
            .Must(value => value!.Trim().Length >= 3)
                .WithMessage("Room name must be at least 3 characters.")
            .Must(value => value!.Trim().Length <= 50)
                .WithMessage("Room name must be 50 characters or fewer.");

        When(request => request.IsPrivate, () =>
        {
            RuleFor(request => request.Password)
                .NotEmpty()
                .MinimumLength(Room.MinimumPasswordLength)
                .MaximumLength(Room.MaximumPasswordLength);
        });
    }
}
