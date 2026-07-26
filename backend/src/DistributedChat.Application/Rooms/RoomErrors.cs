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

    public static ApplicationError OwnerRequired() =>
        ApplicationError.Forbidden("Rooms.OwnerRequired", "Only the room owner can perform this action.");

    public static ApplicationError PasswordRequired() =>
        ApplicationError.Validation("Rooms.PasswordRequired", "A room password is required.");

    public static ApplicationError InvalidPassword() =>
        ApplicationError.Forbidden("Rooms.InvalidPassword", "The room password is invalid.");

    public static ApplicationError OwnerCannotLeave() =>
        ApplicationError.Conflict("Rooms.OwnerCannotLeave", "The room owner cannot leave their own room.");

    public static ApplicationError OwnerCannotBeRemoved() =>
        ApplicationError.Conflict("Rooms.OwnerCannotBeRemoved", "The room owner cannot be removed.");

    public static ApplicationError MemberNotFound() =>
        ApplicationError.NotFound("Rooms.MemberNotFound", "The room member was not found.");

    public static ApplicationError PublicRoomHasNoPassword() =>
        ApplicationError.Conflict("Rooms.PublicRoomHasNoPassword", "Public rooms do not have a password.");

    public static ApplicationError InvalidInvite() =>
        ApplicationError.NotFound("Rooms.InvalidInvite", "The invitation is invalid or no longer active.");
}
