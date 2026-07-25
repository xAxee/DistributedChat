using DistributedChat.Domain.Rooms;
using FluentValidation;

namespace DistributedChat.Api.Dtos;

public sealed class UpdateRoomDtoValidator : AbstractValidator<UpdateRoomDto>
{
    public UpdateRoomDtoValidator()
    {
        RuleFor(request => request.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .Must(value => value!.Trim().Length >= Room.MinimumNameLength)
            .Must(value => value!.Trim().Length <= Room.MaximumNameLength);
    }
}
