using DistributedChat.Domain.ProcessedEvents;
using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.IntegrationTests.Persistence;

[Collection(TestCollections.PostgreSql)]
public sealed class DistributedChatDbContextTests(PostgreSqlFixture fixture) : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        return fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CanSaveAndReadUser()
    {
        await using var dbContext = fixture.CreateDbContext();
        var user = CreateUser("alice", "alice@example.com");

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync();

        dbContext.ChangeTracker.Clear();

        var persistedUser = await dbContext.Users.SingleAsync(item => item.Id == user.Id);

        Assert.Equal(user.Id, persistedUser.Id);
        Assert.Equal("alice", persistedUser.Username);
        Assert.Equal("ALICE", persistedUser.NormalizedUsername);
        Assert.Equal("alice@example.com", persistedUser.Email);
        Assert.Equal("ALICE@EXAMPLE.COM", persistedUser.NormalizedEmail);
        Assert.Equal(TimeSpan.Zero, persistedUser.CreatedAt.Offset);
    }

    [Fact]
    public async Task NormalizedUsernameMustBeUnique()
    {
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(CreateUser("alice", "alice@example.com"));
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(CreateUser("Alice", "alice2@example.com"));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task NormalizedEmailMustBeUnique()
    {
        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(CreateUser("alice", "alice@example.com"));
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(CreateUser("alice2", "Alice@Example.com"));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task RoomMemberCannotBeDuplicated()
    {
        var user = CreateUser("owner", "owner@example.com");
        var room = CreateRoom(user.Id);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            dbContext.Rooms.Add(room);
            dbContext.RoomMembers.Add(CreateRoomMember(room.Id, user.Id));
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.RoomMembers.Add(CreateRoomMember(room.Id, user.Id));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
    }

    [Fact]
    public async Task ProcessedEventKeyIncludesConsumerIdAndEventId()
    {
        var eventId = Guid.NewGuid();

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.ProcessedEvents.Add(CreateProcessedEvent("consumer-a", eventId));
            dbContext.ProcessedEvents.Add(CreateProcessedEvent("consumer-b", eventId));
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var processedEventsCount = await dbContext.ProcessedEvents.CountAsync();

            Assert.Equal(2, processedEventsCount);
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.ProcessedEvents.Add(CreateProcessedEvent("consumer-a", eventId));

            await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
        }
    }

    private static User CreateUser(string username, string email)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            NormalizedUsername = username.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            PasswordHash = "password-hash",
            CreatedAt = DateTimeOffset.UtcNow,
        };
    }

    private static Room CreateRoom(Guid createdByUserId)
    {
        return Room.Create(Guid.NewGuid(), "general", createdByUserId, DateTimeOffset.UtcNow);
    }

    private static RoomMember CreateRoomMember(Guid roomId, Guid userId)
    {
        return new RoomMember
        {
            RoomId = roomId,
            UserId = userId,
            JoinedAt = DateTimeOffset.UtcNow,
        };
    }

    private static ProcessedEvent CreateProcessedEvent(string consumerId, Guid eventId)
    {
        return new ProcessedEvent
        {
            ConsumerId = consumerId,
            EventId = eventId,
            ProcessedAt = DateTimeOffset.UtcNow,
        };
    }
}
