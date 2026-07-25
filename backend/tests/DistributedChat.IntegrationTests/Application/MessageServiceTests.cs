using DistributedChat.Application.Common.Abstractions;
using DistributedChat.Application.Messages;
using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using DistributedChat.Infrastructure.Persistence.Messages;
using DistributedChat.Infrastructure.Persistence.Rooms;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DistributedChat.IntegrationTests.Application;

[Collection(TestCollections.PostgreSql)]
public sealed class MessageServiceTests(PostgreSqlFixture fixture) : IAsyncLifetime
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
    public async Task SendMessagePersistsMessageWithCurrentUserAsSender()
    {
        var user = CreateUser("alice", "alice@example.com");
        var room = CreateRoom(user.Id);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Users.Add(user);
            dbContext.Rooms.Add(room);
            dbContext.RoomMembers.Add(new RoomMember
            {
                RoomId = room.Id,
                UserId = user.Id,
                JoinedAt = new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero),
            });
            await dbContext.SaveChangesAsync();
        }

        await using (var dbContext = fixture.CreateDbContext())
        {
            var publisher = new CapturingChatEventPublisher();
            var now = new DateTimeOffset(2026, 7, 10, 12, 30, 0, TimeSpan.Zero);
            var service = new MessageService(
                new MessageStore(dbContext),
                new RoomStore(dbContext),
                new TestCurrentUser(user.Id, user.Username),
                new FixedTimeProvider(now),
                publisher,
                new SendMessageRequestValidator(),
                NullLogger<MessageService>.Instance);

            var result = await service.SendMessageAsync(new SendMessageRequest(room.Id, "  hello world  "));

            Assert.True(result.IsSuccess, result.Error?.Message);
            Assert.Equal(room.Id, result.Value.RoomId);
            Assert.Equal(user.Id, result.Value.SenderUserId);
            Assert.Equal("alice", result.Value.SenderUsername);
            Assert.Equal("hello world", result.Value.Content);
            Assert.Equal(now, result.Value.CreatedAt);

            var persistedMessage = await dbContext.Messages.AsNoTracking().SingleAsync();
            Assert.Equal(result.Value.Id, persistedMessage.Id);
            Assert.Equal(user.Id, persistedMessage.SenderUserId);
            Assert.Equal("hello world", persistedMessage.Content);

            Assert.NotNull(publisher.PublishedMessage);
            Assert.Equal(result.Value.Id, publisher.PublishedMessage.MessageId);
            Assert.Equal(room.Id, publisher.PublishedMessage.RoomId);
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

    private sealed class TestCurrentUser(Guid userId, string username) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? UserId => userId;

        public string? Username => username;

        public string? Email => null;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class CapturingChatEventPublisher : IChatEventPublisher
    {
        public ChatMessageCreated? PublishedMessage { get; private set; }

        public Task PublishMessageCreatedAsync(
            ChatMessageCreated messageCreated,
            CancellationToken cancellationToken = default
        )
        {
            PublishedMessage = messageCreated;

            return Task.CompletedTask;
        }

        public Task PublishUserJoinedRoomAsync(
            UserRoomPresenceEvent userJoinedRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task PublishUserLeftRoomAsync(
            UserRoomPresenceEvent userLeftRoom,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
