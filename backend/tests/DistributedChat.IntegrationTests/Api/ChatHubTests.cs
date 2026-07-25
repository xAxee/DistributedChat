using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DistributedChat.Api.Dtos;
using DistributedChat.Api.Hubs;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Application.Messages;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.IntegrationTests.Api;

[Collection(TestCollections.PostgreSql)]
public sealed class ChatHubTests(PostgreSqlFixture fixture) : IAsyncLifetime, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private DistributedChatApiFactory? factory;
    private HttpClient? client;

    public async Task InitializeAsync()
    {
        await fixture.ResetDatabaseAsync();
        factory = new DistributedChatApiFactory(fixture);
        client = factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        client?.Dispose();

        if (factory is not null)
        {
            await factory.DisposeAsync();
        }
    }

    public void Dispose()
    {
        client?.Dispose();
        factory?.Dispose();
    }

    [Fact]
    public async Task UserOutsideRoomCannotJoinRoomGroup()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        await using var connection = CreateHubConnection(bob.AccessToken);
        await connection.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id));

        Assert.Contains("Rooms.MembershipRequired", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UserOutsideRoomCannotLeaveRoomGroup()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        await using var connection = CreateHubConnection(bob.AccessToken);
        await connection.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(nameof(ChatHub.LeaveRoom), room.Id));

        Assert.Contains("Rooms.MembershipRequired", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CannotLeaveMissingRoomGroup()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        await using var connection = CreateHubConnection(alice.AccessToken);
        await connection.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync(nameof(ChatHub.LeaveRoom), Guid.NewGuid()));

        Assert.Contains("Rooms.NotFound", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SendMessagePersistsMessage()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        await using var connection = CreateHubConnection(alice.AccessToken);
        await connection.StartAsync();

        var sentMessage = await connection.InvokeAsync<MessageDto>(
            nameof(ChatHub.SendMessage),
            new SendMessageDto(room.Id, "  hello from signalr  "));

        await using var dbContext = fixture.CreateDbContext();
        var message = await dbContext.Messages.AsNoTracking().SingleAsync();

        Assert.Equal(room.Id, message.RoomId);
        Assert.Equal(alice.User.Id, message.SenderUserId);
        Assert.Equal("hello from signalr", message.Content);
        Assert.Equal(message.Id, sentMessage.Id);
        Assert.Equal(message.Content, sentMessage.Content);
    }

    [Fact]
    public async Task MessageReceivedIsDeliveredOnceToGroupAndNotToClientOutsideGroup()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);

        await using var aliceConnection = CreateHubConnection(alice.AccessToken);
        await using var bobConnection = CreateHubConnection(bob.AccessToken);

        var aliceMessageReceived = new TaskCompletionSource<ChatMessageCreated>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var aliceReceivedCount = 0;
        var bobReceivedCount = 0;

        aliceConnection.On<ChatMessageCreated>(ChatHubEvents.MessageReceived, message =>
        {
            Interlocked.Increment(ref aliceReceivedCount);
            aliceMessageReceived.TrySetResult(message);
        });
        bobConnection.On<ChatMessageCreated>(
            ChatHubEvents.MessageReceived,
            _ => Interlocked.Increment(ref bobReceivedCount));

        await aliceConnection.StartAsync();
        await bobConnection.StartAsync();
        await aliceConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);

        await aliceConnection.InvokeAsync(
            nameof(ChatHub.SendMessage),
            new SendMessageDto(room.Id, "hello group"));

        var received = await WaitForAsync(aliceMessageReceived.Task);
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, Volatile.Read(ref aliceReceivedCount));
        Assert.Equal(0, Volatile.Read(ref bobReceivedCount));
        Assert.Equal(room.Id, received.RoomId);
        Assert.Equal(alice.User.Id, received.SenderUserId);
        Assert.Equal("alice", received.SenderUsername);
        Assert.Equal("hello group", received.Content);
        Assert.NotEqual(Guid.Empty, received.EventId);
        Assert.NotEqual(Guid.Empty, received.MessageId);
        Assert.True(received.CreatedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task LeaveRoomWithoutSubscriptionDoesNotPublishUserLeftRoom()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);

        await using var aliceConnection = CreateHubConnection(alice.AccessToken);
        await using var bobConnection = CreateHubConnection(bob.AccessToken);

        var bobLeftRoomCount = 0;
        bobConnection.On<UserRoomPresenceEvent>(
            ChatHubEvents.UserLeftRoom,
            _ => Interlocked.Increment(ref bobLeftRoomCount));

        await bobConnection.StartAsync();
        await bobConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);
        await aliceConnection.StartAsync();

        await aliceConnection.InvokeAsync(nameof(ChatHub.LeaveRoom), room.Id);
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(0, Volatile.Read(ref bobLeftRoomCount));
    }

    [Fact]
    public async Task LeaveRoomPublishesUserLeftRoomOnceForSubscribedConnection()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);

        await using var aliceConnection = CreateHubConnection(alice.AccessToken);
        await using var bobConnection = CreateHubConnection(bob.AccessToken);

        var userLeftRoom = new TaskCompletionSource<UserRoomPresenceEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var bobLeftRoomCount = 0;
        bobConnection.On<UserRoomPresenceEvent>(ChatHubEvents.UserLeftRoom, presenceEvent =>
        {
            Interlocked.Increment(ref bobLeftRoomCount);
            userLeftRoom.TrySetResult(presenceEvent);
        });

        await bobConnection.StartAsync();
        await bobConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);
        await aliceConnection.StartAsync();
        await aliceConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);

        await aliceConnection.InvokeAsync(nameof(ChatHub.LeaveRoom), room.Id);

        var received = await WaitForAsync(userLeftRoom.Task);
        await Task.Delay(TimeSpan.FromMilliseconds(250));

        Assert.Equal(1, Volatile.Read(ref bobLeftRoomCount));
        Assert.NotEqual(Guid.Empty, received.EventId);
        Assert.Equal(room.Id, received.RoomId);
        Assert.Equal(alice.User.Id, received.UserId);
        Assert.Equal("alice", received.Username);
        Assert.False(string.IsNullOrWhiteSpace(received.ConnectionId));
        Assert.Equal("test-api", received.InstanceId);
    }

    [Fact]
    public async Task DisconnectPublishesUserLeftRoomForSubscribedConnection()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);

        await using var aliceConnection = CreateHubConnection(alice.AccessToken);
        await using var bobConnection = CreateHubConnection(bob.AccessToken);

        var disconnectLeftRoom = new TaskCompletionSource<UserRoomPresenceEvent>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var bobLeftRoomCount = 0;
        bobConnection.On<UserRoomPresenceEvent>(
            ChatHubEvents.UserLeftRoom,
            presenceEvent =>
            {
                Interlocked.Increment(ref bobLeftRoomCount);
                disconnectLeftRoom.TrySetResult(presenceEvent);
            });

        await bobConnection.StartAsync();
        await bobConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);
        await aliceConnection.StartAsync();
        await aliceConnection.InvokeAsync(nameof(ChatHub.JoinRoom), room.Id);

        await aliceConnection.StopAsync();
        var received = await WaitForAsync(disconnectLeftRoom.Task);

        Assert.Equal(1, Volatile.Read(ref bobLeftRoomCount));
        Assert.NotEqual(Guid.Empty, received.EventId);
        Assert.Equal(room.Id, received.RoomId);
        Assert.Equal(alice.User.Id, received.UserId);
        Assert.False(string.IsNullOrWhiteSpace(received.ConnectionId));
        Assert.Equal("test-api", received.InstanceId);

        UseToken(alice.AccessToken);
        var aliceRoom = await ReadSuccessAsync<RoomDetailsDto>(await Client.GetAsync($"/api/rooms/{room.Id}"));
        Assert.True(aliceRoom.IsMember);
    }

    [Fact]
    public async Task ConnectedRoomMemberIsReportedOnlineWithInstanceId()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        await using var connection = CreateHubConnection(alice.AccessToken);
        await connection.StartAsync();

        var onlineMember = await WaitForMemberPresenceAsync(room.Id, expectedOnline: true);
        Assert.Equal(alice.User.Id, onlineMember.UserId);
        Assert.True(onlineMember.IsOnline);
        Assert.Contains("test-api", onlineMember.ConnectedInstanceIds);

        await connection.StopAsync();

        var offlineMember = await WaitForMemberPresenceAsync(room.Id, expectedOnline: false);
        Assert.False(offlineMember.IsOnline);
        Assert.Empty(offlineMember.ConnectedInstanceIds);
    }

    private HttpClient Client => client ?? throw new InvalidOperationException("Test client is not initialized.");

    private DistributedChatApiFactory Factory =>
        factory ?? throw new InvalidOperationException("Test factory is not initialized.");

    private void UseToken(string accessToken)
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }

    private async Task<AuthResponse> RegisterAsync(string email, string username)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto(email, username, "password123"));

        return await ReadSuccessAsync<AuthResponse>(response);
    }

    private async Task<RoomDetailsDto> CreateRoomAsync(string name)
    {
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomDto(name));

        return await ReadSuccessAsync<RoomDetailsDto>(response);
    }

    private HubConnection CreateHubConnection(string accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl(new Uri(Client.BaseAddress!, "/hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
            })
            .Build();
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);

        return JsonSerializer.Deserialize<T>(content, JsonOptions)
            ?? throw new InvalidOperationException("Response body was empty.");
    }

    private static async Task<T> WaitForAsync<T>(Task<T> task)
    {
        var completedTask = await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(5)));
        if (completedTask != task)
        {
            throw new TimeoutException("Timed out waiting for SignalR event.");
        }

        return await task;
    }

    private async Task<RoomMemberDto> WaitForMemberPresenceAsync(Guid roomId, bool expectedOnline)
    {
        var deadline = DateTimeOffset.UtcNow.AddSeconds(5);

        while (DateTimeOffset.UtcNow < deadline)
        {
            var members = await ReadSuccessAsync<IReadOnlyCollection<RoomMemberDto>>(
                await Client.GetAsync($"/api/rooms/{roomId}/members"));
            var member = Assert.Single(members);

            if (member.IsOnline == expectedOnline)
            {
                return member;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100));
        }

        throw new TimeoutException($"Timed out waiting for room member online state '{expectedOnline}'.");
    }
}
