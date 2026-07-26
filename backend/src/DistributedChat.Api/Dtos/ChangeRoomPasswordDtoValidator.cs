using DistributedChat.Domain.Rooms;
using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class ChangeRoomPasswordDtoValidator : AbstractValidator<ChangeRoomPasswordDto>
{
    public ChangeRoomPasswordDtoValidator()
    {
        RuleFor(request => request.Password)
            .NotEmpty()
            .Must(password => !string.IsNullOrWhiteSpace(password))
            .MinimumLength(Room.MinimumPasswordLength)
            .MaximumLength(Room.MaximumPasswordLength);
    }
}
