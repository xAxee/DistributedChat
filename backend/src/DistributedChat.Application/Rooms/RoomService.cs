using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;
using DistributedChat.Application.Presence;
using DistributedChat.Domain.Rooms;

namespace DistributedChat.Application.Rooms;

public sealed class RoomService(
    IRoomStore roomStore,
    IUserAccountStore userAccountStore,
    TimeProvider timeProvider,
    ICurrentUser currentUser
) : IRoomService
{
    public async Task<Result<RoomDetailsDto>> CreateRoomAsync(CreateRoomRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.Unauthenticated());
        }

        if (await userAccountStore.FindByIdAsync(currentUserId.Value) is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.CurrentUserNotFound());
        }

        var now = timeProvider.GetUtcNow();
        var room = Room.Create(
            Guid.NewGuid(),
            request.Name!,
            currentUserId.Value,
            now);

        var creatorMembership = new RoomMember
        {
            RoomId = room.Id,
            UserId = currentUserId.Value,
            JoinedAt = now,
        };

        var createdRoom = await roomStore.CreateAsync(room, creatorMembership);

        return Result.Success(createdRoom);
    }

    public async Task<Result<IReadOnlyCollection<RoomSummaryDto>>> GetRoomsAsync()
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<IReadOnlyCollection<RoomSummaryDto>>(RoomErrors.Unauthenticated());
        }

        var rooms = await roomStore.ListAsync();

        return Result.Success(rooms);
    }

    public async Task<Result<RoomDetailsDto>> GetRoomAsync(Guid roomId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.Unauthenticated());
        }

        var room = await roomStore.GetDetailsAsync(roomId, currentUserId.Value);
        if (room is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.NotFound());
        }

        return Result.Success(room);
    }

    public async Task<Result> JoinRoomAsync(Guid roomId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure(RoomErrors.Unauthenticated());
        }

        if (!await roomStore.ExistsAsync(roomId))
        {
            return Result.Failure(RoomErrors.NotFound());
        }

        await roomStore.JoinAsync(roomId, currentUserId.Value, timeProvider.GetUtcNow());

        return Result.Success();
    }

    public async Task<Result> LeaveRoomAsync(Guid roomId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure(RoomErrors.Unauthenticated());
        }

        var membership = await EnsureMembershipAsync(roomId, currentUserId.Value);
        if (membership.IsFailure)
        {
            return Result.Failure(membership.Error!);
        }

        await roomStore.LeaveAsync(roomId, currentUserId.Value);

        return Result.Success();
    }

    public async Task<Result<IReadOnlyCollection<RoomMemberDto>>> GetRoomMembersAsync(Guid roomId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<IReadOnlyCollection<RoomMemberDto>>(RoomErrors.Unauthenticated());
        }

        var membership = await EnsureMembershipAsync(roomId, currentUserId.Value);
        if (membership.IsFailure)
        {
            return Result.Failure<IReadOnlyCollection<RoomMemberDto>>(membership.Error!);
        }

        var members = await roomStore.GetMembersAsync(
            roomId,
            timeProvider.GetUtcNow().Subtract(UserPresenceDefaults.OfflineAfter));

        return Result.Success(members);
    }

    private async Task<Result> EnsureMembershipAsync(Guid roomId, Guid userId)
    {
        if (!await roomStore.ExistsAsync(roomId))
        {
            return Result.Failure(RoomErrors.NotFound());
        }

        if (!await roomStore.IsMemberAsync(roomId, userId))
        {
            return Result.Failure(RoomErrors.MembershipRequired());
        }

        return Result.Success();
    }

    private Guid? GetCurrentUserId()
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId is null)
        {
            return null;
        }

        return currentUser.UserId.Value;
    }
}
