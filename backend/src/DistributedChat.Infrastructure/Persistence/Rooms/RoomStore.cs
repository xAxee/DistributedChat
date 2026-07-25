using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Rooms;
using DistributedChat.Domain.Rooms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DistributedChat.Infrastructure.Persistence.Rooms;

public sealed class RoomStore(DistributedChatDbContext dbContext) : IRoomStore
{
    private const string RoomMemberPrimaryKeyName = "pk_room_members";

    public async Task<RoomDetailsDto> CreateAsync(
        Room room,
        RoomMember creatorMembership,
        CancellationToken cancellationToken = default
    )
    {
        dbContext.Rooms.Add(room);
        dbContext.RoomMembers.Add(creatorMembership);

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RoomDetailsDto(room.Id, room.Name, room.CreatedByUserId, room.CreatedAt, IsMember: true);
    }

    public async Task<IReadOnlyCollection<RoomSummaryDto>> ListAsync(
        CancellationToken cancellationToken = default
    )
    {
        return await dbContext.Rooms
            .AsNoTracking()
            .OrderByDescending(room => room.CreatedAt)
            .ThenByDescending(room => room.Id)
            .Select(room => new RoomSummaryDto(room.Id, room.Name, room.CreatedByUserId, room.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public Task<RoomDetailsDto?> GetDetailsAsync(
        Guid roomId,
        Guid currentUserId,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.Rooms
            .AsNoTracking()
            .Where(room => room.Id == roomId)
            .Select(room => new RoomDetailsDto(
                room.Id,
                room.Name,
                room.CreatedByUserId,
                room.CreatedAt,
                dbContext.RoomMembers
                    .AsNoTracking()
                    .Any(member => member.RoomId == room.Id && member.UserId == currentUserId)))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> ExistsAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        return dbContext.Rooms
            .AsNoTracking()
            .AnyAsync(room => room.Id == roomId, cancellationToken);
    }

    public Task<bool> IsMemberAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return dbContext.RoomMembers
            .AsNoTracking()
            .AnyAsync(member => member.RoomId == roomId && member.UserId == userId, cancellationToken);
    }

    public async Task JoinAsync(
        Guid roomId,
        Guid userId,
        DateTimeOffset joinedAt,
        CancellationToken cancellationToken = default
    )
    {
        if (await IsMemberAsync(roomId, userId, cancellationToken))
        {
            return;
        }

        var member = new RoomMember
        {
            RoomId = roomId,
            UserId = userId,
            JoinedAt = joinedAt,
        };

        dbContext.RoomMembers.Add(member);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsRoomMemberDuplicate(exception))
        {
            dbContext.Entry(member).State = EntityState.Detached;
        }
    }

    public async Task LeaveAsync(
        Guid roomId,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        await dbContext.RoomMembers
            .Where(member => member.RoomId == roomId && member.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<RoomMemberDto>> GetMembersAsync(
        Guid roomId,
        DateTimeOffset onlineAfter,
        CancellationToken cancellationToken = default
    )
    {
        var members = await dbContext.RoomMembers
            .AsNoTracking()
            .Where(member => member.RoomId == roomId)
            .Join(
                dbContext.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new
                {
                    member.RoomId,
                    UserId = user.Id,
                    user.Username,
                    member.JoinedAt,
                })
            .OrderBy(member => member.JoinedAt)
            .ThenBy(member => member.Username)
            .ToListAsync(cancellationToken);

        var userIds = members.Select(member => member.UserId).ToArray();
        var activePresences = await dbContext.UserPresences
            .AsNoTracking()
            .Where(presence => userIds.Contains(presence.UserId)
                && presence.ConnectionCount > 0
                && presence.LastHeartbeatAt >= onlineAfter)
            .OrderBy(presence => presence.InstanceId)
            .Select(presence => new
            {
                presence.UserId,
                presence.InstanceId,
            })
            .ToListAsync(cancellationToken);

        var activeInstancesByUserId = activePresences
            .GroupBy(presence => presence.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(presence => presence.InstanceId).ToArray() as IReadOnlyCollection<string>);

        return members
            .Select(member =>
            {
                activeInstancesByUserId.TryGetValue(member.UserId, out var instanceIds);
                instanceIds ??= [];

                return new RoomMemberDto(
                    member.RoomId,
                    member.UserId,
                    member.Username,
                    member.JoinedAt,
                    instanceIds.Count > 0,
                    instanceIds);
            })
            .ToList();
    }

    private static bool IsRoomMemberDuplicate(DbUpdateException exception)
    {
        return exception.InnerException is PostgresException postgresException
            && postgresException.SqlState == PostgresErrorCodes.UniqueViolation
            && postgresException.ConstraintName == RoomMemberPrimaryKeyName;
    }
}
