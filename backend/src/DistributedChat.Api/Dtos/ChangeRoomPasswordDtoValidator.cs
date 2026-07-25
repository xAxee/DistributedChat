using DistributedChat.Domain.Rooms;
using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class ChangeRoomPasswordDtoValidator : AbstractValidator<ChangeRoomPasswordDto>
{
    public ChangeRoomPasswordDtoValidator()
    {
        RuleFor(request => request.Password)
            .NotEmpty()
            .MinimumLength(Room.MinimumPasswordLength)
            .MaximumLength(Room.MaximumPasswordLength);
    }
}
