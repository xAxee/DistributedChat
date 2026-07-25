using DistributedChat.Application.Common.Results;

namespace DistributedChat.Application.Rooms;

public static class RoomErrors
{
    public static ApplicationError Unauthenticated() =>
        ApplicationError.Unauthorized("Auth.Unauthenticated", "Authentication is required.");

    public static ApplicationError CurrentUserNotFound() =>
        ApplicationError.NotFound("Users.CurrentUserNotFound", "Current user was not found.");

    public static ApplicationError NotFound() =>
        ApplicationError.NotFound("Rooms.NotFound", "Room was not found.");

    public static ApplicationError MembershipRequired() =>
        ApplicationError.Forbidden("Rooms.MembershipRequired", "Room membership is required.");
}
