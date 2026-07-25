using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Messages;

public static class MessageErrors
{
    public static ApplicationError InvalidLimit() =>
        ApplicationError.Validation("Messages.InvalidLimit", "Message history limit must be between 1 and 100.");

    public static ApplicationError InvalidCursor() =>
        ApplicationError.NotFound("Messages.InvalidCursor", "Message history cursor was not found.");
}
