using DistributedChat.Domain.Messages;
using FluentValidation;

namespace DistributedChat.Application.Messages;

public sealed class SendMessageRequestValidator : AbstractValidator<SendMessageRequest>
{
    public SendMessageRequestValidator()
    {
        RuleFor(request => request.RoomId)
            .NotEmpty()
                .WithMessage("RoomId is required.");

        RuleFor(request => request.Content)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
                .WithMessage("Message content is required.")
            .Must(value => value!.Trim().Length <= Message.MaximumContentLength)
                .WithMessage($"Message content must be {Message.MaximumContentLength} characters or fewer.");
    }
}
