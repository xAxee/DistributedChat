using DistributedChat.Application.Rooms;
using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Infrastructure.Persistence;

public static class DistributedChatDbSeeder
{
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private const string SystemUsername = "System";
    private const string SystemNormalizedUsername = "SYSTEM";
    private const string SystemEmail = "system@distributed.chat";
    private const string SystemNormalizedEmail = "SYSTEM@DISTRIBUTED.CHAT";

    public static async Task SeedGlobalRoomAsync(
        DistributedChatDbContext dbContext,
        CancellationToken cancellationToken = default
    )
    {
        var systemUser = await GetOrCreateSystemUserAsync(dbContext, cancellationToken);
        var globalRoom = await EnsureGlobalRoomAsync(dbContext, systemUser.Id, cancellationToken);
        await EnsureGlobalMembershipsAsync(dbContext, globalRoom.Id, systemUser.Id, cancellationToken);
    }

    private static async Task<User> GetOrCreateSystemUserAsync(
        DistributedChatDbContext dbContext,
        CancellationToken cancellationToken
    )
    {
        var systemUser = await dbContext.Users
            .SingleOrDefaultAsync(
                user => user.Id == SystemUserId || user.NormalizedEmail == SystemNormalizedEmail,
                cancellationToken);

        if (systemUser is not null)
        {
            return systemUser;
        }

        var passwordHasher = new PasswordHasher<User>();
        systemUser = new User
        {
            Id = SystemUserId,
            Username = SystemUsername,
            NormalizedUsername = SystemNormalizedUsername,
            Email = SystemEmail,
            NormalizedEmail = SystemNormalizedEmail,
            CreatedAt = SeededAt,
            PasswordHash = passwordHasher.HashPassword(user: null!, Guid.NewGuid().ToString("N")),
        };

        dbContext.Users.Add(systemUser);
        await dbContext.SaveChangesAsync(cancellationToken);

        return systemUser;
    }

    private static async Task<Room> EnsureGlobalRoomAsync(
        DistributedChatDbContext dbContext,
        Guid createdByUserId,
        CancellationToken cancellationToken
    )
    {
        var globalRoom = await dbContext.Rooms
            .SingleOrDefaultAsync(
                room => room.Id == GlobalRoomDefaults.Id || room.Name == GlobalRoomDefaults.Name,
                cancellationToken);

        if (globalRoom is not null)
        {
            return globalRoom;
        }

        globalRoom = Room.Create(
            GlobalRoomDefaults.Id,
            GlobalRoomDefaults.Name,
            createdByUserId,
            SeededAt);
        dbContext.Rooms.Add(globalRoom);
        await dbContext.SaveChangesAsync(cancellationToken);
        return globalRoom;
    }

    private static async Task EnsureGlobalMembershipsAsync(
        DistributedChatDbContext dbContext,
        Guid globalRoomId,
        Guid systemUserId,
        CancellationToken cancellationToken)
    {
        var memberIds = await dbContext.RoomMembers
            .Where(member => member.RoomId == globalRoomId)
            .Select(member => member.UserId)
            .ToListAsync(cancellationToken);

        var missingUserIds = await dbContext.Users
            .Where(user => user.Id != systemUserId && !memberIds.Contains(user.Id))
            .Select(user => user.Id)
            .ToListAsync(cancellationToken);

        dbContext.RoomMembers.AddRange(missingUserIds.Select(userId => new RoomMember
        {
            RoomId = globalRoomId,
            UserId = userId,
            JoinedAt = SeededAt,
        }));

        if (missingUserIds.Count > 0)
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
