using System.Security.Cryptography;
using System.Text;
using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Common.Results;
using DistributedChat.Application.Presence;
using DistributedChat.Domain.Rooms;

namespace DistributedChat.Application.Rooms;

public sealed class RoomService(
    IRoomStore roomStore,
    IUserAccountStore userAccountStore,
    IPasswordHasher passwordHasher,
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
        var passwordHash = request.IsPrivate
            ? passwordHasher.HashPassword(request.Password!)
            : null;
        var room = Room.Create(
            Guid.NewGuid(),
            request.Name!,
            currentUserId.Value,
            now,
            request.IsPrivate,
            passwordHash);

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

        var rooms = await roomStore.ListAsync(currentUserId.Value);

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

    public async Task<Result> JoinRoomAsync(Guid roomId, JoinRoomRequest request)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure(RoomErrors.Unauthenticated());
        }

        var room = await roomStore.GetAsync(roomId);
        if (room is null)
        {
            return Result.Failure(RoomErrors.NotFound());
        }

        if (room.IsPrivate
            && (string.IsNullOrWhiteSpace(request.Password)
                || !passwordHasher.VerifyPassword(room.PasswordHash!, request.Password)))
        {
            return Result.Failure(
                string.IsNullOrWhiteSpace(request.Password)
                    ? RoomErrors.PasswordRequired()
                    : RoomErrors.InvalidPassword());
        }

        await roomStore.JoinAsync(roomId, currentUserId.Value, timeProvider.GetUtcNow());

        return Result.Success();
    }

    public async Task<Result<RoomDetailsDto>> JoinRoomByInviteAsync(string token)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.Unauthenticated());
        }

        var room = await roomStore.GetByInviteTokenHashAsync(HashInviteToken(token));
        if (room is null)
        {
            return Result.Failure<RoomDetailsDto>(RoomErrors.InvalidInvite());
        }

        await roomStore.JoinAsync(room.Id, currentUserId.Value, timeProvider.GetUtcNow());
        var details = await roomStore.GetDetailsAsync(room.Id, currentUserId.Value);

        return Result.Success(details!);
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

        var room = await roomStore.GetAsync(roomId);
        if (room!.CreatedByUserId == currentUserId.Value)
        {
            return Result.Failure(RoomErrors.OwnerCannotLeave());
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

    public async Task<Result<RoomDetailsDto>> UpdateRoomAsync(Guid roomId, UpdateRoomRequest request)
    {
        var ownership = await GetOwnedRoomAsync(roomId);
        if (ownership.IsFailure)
        {
            return Result.Failure<RoomDetailsDto>(ownership.Error!);
        }

        ownership.Value.Rename(request.Name!);
        await roomStore.SaveChangesAsync();

        var details = await roomStore.GetDetailsAsync(roomId, ownership.Value.CreatedByUserId);
        return Result.Success(details!);
    }

    public async Task<Result> ChangeRoomPasswordAsync(
        Guid roomId,
        ChangeRoomPasswordRequest request
    )
    {
        var ownership = await GetOwnedRoomAsync(roomId);
        if (ownership.IsFailure)
        {
            return Result.Failure(ownership.Error!);
        }

        if (!ownership.Value.IsPrivate)
        {
            return Result.Failure(RoomErrors.PublicRoomHasNoPassword());
        }

        ownership.Value.ChangePassword(passwordHasher.HashPassword(request.Password!));
        await roomStore.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result> RemoveRoomMemberAsync(Guid roomId, Guid userId)
    {
        var ownership = await GetOwnedRoomAsync(roomId);
        if (ownership.IsFailure)
        {
            return Result.Failure(ownership.Error!);
        }

        if (ownership.Value.CreatedByUserId == userId)
        {
            return Result.Failure(RoomErrors.OwnerCannotBeRemoved());
        }

        if (!await roomStore.IsMemberAsync(roomId, userId))
        {
            return Result.Failure(RoomErrors.MemberNotFound());
        }

        await roomStore.LeaveAsync(roomId, userId);
        return Result.Success();
    }

    public async Task<Result> DeleteRoomAsync(Guid roomId)
    {
        var ownership = await GetOwnedRoomAsync(roomId);
        if (ownership.IsFailure)
        {
            return Result.Failure(ownership.Error!);
        }

        await roomStore.DeleteAsync(roomId);
        return Result.Success();
    }

    public async Task<Result<RoomInviteDto>> GenerateInviteAsync(Guid roomId)
    {
        var ownership = await GetOwnedRoomAsync(roomId);
        if (ownership.IsFailure)
        {
            return Result.Failure<RoomInviteDto>(ownership.Error!);
        }

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');

        ownership.Value.SetInviteTokenHash(HashInviteToken(token));
        await roomStore.SaveChangesAsync();

        return Result.Success(new RoomInviteDto(token));
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

    private async Task<Result<Room>> GetOwnedRoomAsync(Guid roomId)
    {
        var currentUserId = GetCurrentUserId();
        if (currentUserId is null)
        {
            return Result.Failure<Room>(RoomErrors.Unauthenticated());
        }

        var room = await roomStore.GetAsync(roomId);
        if (room is null)
        {
            return Result.Failure<Room>(RoomErrors.NotFound());
        }

        return room.CreatedByUserId == currentUserId.Value
            ? Result.Success(room)
            : Result.Failure<Room>(RoomErrors.OwnerRequired());
    }

    private static string HashInviteToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return string.Empty;
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
