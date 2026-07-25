using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DistributedChat.Api.Dtos;
using DistributedChat.Application.Common.Dto;
using DistributedChat.Domain.Messages;
using DistributedChat.IntegrationTests.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.IntegrationTests.Api;

[Collection(TestCollections.PostgreSql)]
public sealed class RoomsEndpointTests(PostgreSqlFixture fixture) : IAsyncLifetime, IDisposable
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
    public async Task RoomsRequireJwt()
    {
        var response = await Client.GetAsync("/api/rooms");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Authentication is required.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task CreateRoomAddsCreatorAsMember()
    {
        var auth = await RegisterAsync("alice@example.com", "alice");
        UseToken(auth.AccessToken);

        var created = await CreateRoomAsync("  general  ");

        Assert.Equal("general", created.Name);
        Assert.Equal(auth.User.Id, created.CreatedByUserId);
        Assert.True(created.IsMember);

        var membersResponse = await Client.GetAsync($"/api/rooms/{created.Id}/members");
        var members = await ReadSuccessAsync<IReadOnlyCollection<RoomMemberDto>>(membersResponse);

        var member = Assert.Single(members);
        Assert.Equal(created.Id, member.RoomId);
        Assert.Equal(auth.User.Id, member.UserId);
        Assert.Equal("alice", member.Username);
    }

    [Fact]
    public async Task JoinIsIdempotentAndLeaveRequiresMembership()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/join", null)).StatusCode);

        var bobRoom = await ReadSuccessAsync<RoomDetailsDto>(await Client.GetAsync($"/api/rooms/{room.Id}"));
        Assert.True(bobRoom.IsMember);

        Assert.Equal(HttpStatusCode.NoContent, (await Client.PostAsync($"/api/rooms/{room.Id}/leave", null)).StatusCode);
        await ReadProblemAsync(
            await Client.PostAsync($"/api/rooms/{room.Id}/leave", null),
            HttpStatusCode.Forbidden);

        bobRoom = await ReadSuccessAsync<RoomDetailsDto>(await Client.GetAsync($"/api/rooms/{room.Id}"));
        Assert.False(bobRoom.IsMember);

        UseToken(alice.AccessToken);
        var members = await ReadSuccessAsync<IReadOnlyCollection<RoomMemberDto>>(
            await Client.GetAsync($"/api/rooms/{room.Id}/members"));
        Assert.DoesNotContain(members, member => member.UserId == bob.User.Id);
    }

    [Fact]
    public async Task PrivateRoomIsHiddenAndRequiresItsPassword()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("private room", isPrivate: true, password: "secret123");

        Assert.True(room.IsPrivate);

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);

        var rooms = await ReadSuccessAsync<IReadOnlyCollection<RoomSummaryDto>>(
            await Client.GetAsync("/api/rooms"));
        Assert.DoesNotContain(rooms, candidate => candidate.Id == room.Id);

        await ReadProblemAsync(
            await Client.PostAsJsonAsync($"/api/rooms/{room.Id}/join", new JoinRoomDto()),
            HttpStatusCode.BadRequest);
        await ReadProblemAsync(
            await Client.PostAsJsonAsync(
                $"/api/rooms/{room.Id}/join",
                new JoinRoomDto("incorrect")),
            HttpStatusCode.Forbidden);

        var joinResponse = await Client.PostAsJsonAsync(
            $"/api/rooms/{room.Id}/join",
            new JoinRoomDto("secret123"));
        Assert.Equal(HttpStatusCode.NoContent, joinResponse.StatusCode);

        rooms = await ReadSuccessAsync<IReadOnlyCollection<RoomSummaryDto>>(
            await Client.GetAsync("/api/rooms"));
        var joinedRoom = Assert.Single(rooms, candidate => candidate.Id == room.Id);
        Assert.True(joinedRoom.IsPrivate);
        Assert.True(joinedRoom.IsMember);
    }

    [Fact]
    public async Task InvitationBypassesPasswordAndRotationInvalidatesPreviousToken()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("private room", isPrivate: true, password: "secret123");

        var firstInvite = await ReadSuccessAsync<RoomInviteDto>(
            await Client.PostAsync($"/api/rooms/{room.Id}/invite", null));
        var secondInvite = await ReadSuccessAsync<RoomInviteDto>(
            await Client.PostAsync($"/api/rooms/{room.Id}/invite", null));

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);

        await ReadProblemAsync(
            await Client.PostAsync($"/api/rooms/invitations/{firstInvite.Token}/join", null),
            HttpStatusCode.NotFound);

        var joined = await ReadSuccessAsync<RoomDetailsDto>(
            await Client.PostAsync($"/api/rooms/invitations/{secondInvite.Token}/join", null));
        Assert.Equal(room.Id, joined.Id);
        Assert.True(joined.IsMember);
    }

    [Fact]
    public async Task OwnerCanRenameChangePasswordRemoveMemberAndDeleteRoom()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("private room", isPrivate: true, password: "secret123");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await Client.PostAsJsonAsync(
                $"/api/rooms/{room.Id}/join",
                new JoinRoomDto("secret123"))).StatusCode);

        await ReadProblemAsync(
            await Client.PutAsJsonAsync($"/api/rooms/{room.Id}", new UpdateRoomDto("renamed room")),
            HttpStatusCode.Forbidden);

        UseToken(alice.AccessToken);
        var renamed = await ReadSuccessAsync<RoomDetailsDto>(
            await Client.PutAsJsonAsync($"/api/rooms/{room.Id}", new UpdateRoomDto("renamed room")));
        Assert.Equal("renamed room", renamed.Name);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await Client.PutAsJsonAsync(
                $"/api/rooms/{room.Id}/password",
                new ChangeRoomPasswordDto("newSecret123"))).StatusCode);

        Assert.Equal(
            HttpStatusCode.NoContent,
            (await Client.DeleteAsync($"/api/rooms/{room.Id}/members/{bob.User.Id}")).StatusCode);

        UseToken(bob.AccessToken);
        await ReadProblemAsync(
            await Client.GetAsync($"/api/rooms/{room.Id}/members"),
            HttpStatusCode.Forbidden);
        await ReadProblemAsync(
            await Client.PostAsJsonAsync(
                $"/api/rooms/{room.Id}/join",
                new JoinRoomDto("secret123")),
            HttpStatusCode.Forbidden);
        Assert.Equal(
            HttpStatusCode.NoContent,
            (await Client.PostAsJsonAsync(
                $"/api/rooms/{room.Id}/join",
                new JoinRoomDto("newSecret123"))).StatusCode);

        UseToken(alice.AccessToken);
        Assert.Equal(HttpStatusCode.NoContent, (await Client.DeleteAsync($"/api/rooms/{room.Id}")).StatusCode);
        await ReadProblemAsync(await Client.GetAsync($"/api/rooms/{room.Id}"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task OwnerCannotLeaveOrRemoveThemself()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        await ReadProblemAsync(
            await Client.PostAsync($"/api/rooms/{room.Id}/leave", null),
            HttpStatusCode.Conflict);
        await ReadProblemAsync(
            await Client.DeleteAsync($"/api/rooms/{room.Id}/members/{alice.User.Id}"),
            HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task MembersAndHistoryRequireMembership()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var bob = await RegisterAsync("bob@example.com", "bob");
        UseToken(bob.AccessToken);

        await ReadProblemAsync(await Client.GetAsync($"/api/rooms/{room.Id}/members"), HttpStatusCode.Forbidden);
        await ReadProblemAsync(await Client.GetAsync($"/api/rooms/{room.Id}/messages"), HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task MessageHistoryUsesCursorPaginationAndExpectedSortOrder()
    {
        var alice = await RegisterAsync("alice@example.com", "alice");
        UseToken(alice.AccessToken);
        var room = await CreateRoomAsync("general");

        var older = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var sameCreatedLowerId = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var sameCreatedHigherId = Guid.Parse("00000000-0000-0000-0000-000000000003");
        var olderCreatedAt = new DateTimeOffset(2026, 7, 10, 10, 0, 0, TimeSpan.Zero);
        var newerCreatedAt = new DateTimeOffset(2026, 7, 10, 11, 0, 0, TimeSpan.Zero);

        await using (var dbContext = fixture.CreateDbContext())
        {
            dbContext.Messages.AddRange(
                CreateMessage(older, room.Id, alice.User.Id, "older", olderCreatedAt),
                CreateMessage(sameCreatedLowerId, room.Id, alice.User.Id, "newer-lower", newerCreatedAt),
                CreateMessage(sameCreatedHigherId, room.Id, alice.User.Id, "newer-higher", newerCreatedAt));
            await dbContext.SaveChangesAsync();
        }

        var firstPage = await ReadSuccessAsync<CursorPagedResponse<MessageDto>>(
            await Client.GetAsync($"/api/rooms/{room.Id}/messages?limit=2"));

        Assert.True(firstPage.HasMore);
        Assert.Equal(sameCreatedLowerId, firstPage.NextCursor);
        Assert.Equal([sameCreatedHigherId, sameCreatedLowerId], firstPage.Items.Select(item => item.Id));

        var secondPage = await ReadSuccessAsync<CursorPagedResponse<MessageDto>>(
            await Client.GetAsync($"/api/rooms/{room.Id}/messages?before={firstPage.NextCursor}&limit=2"));

        Assert.False(secondPage.HasMore);
        Assert.Null(secondPage.NextCursor);
        var message = Assert.Single(secondPage.Items);
        Assert.Equal(older, message.Id);
    }

    private HttpClient Client => client ?? throw new InvalidOperationException("Test client is not initialized.");

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

    private async Task<RoomDetailsDto> CreateRoomAsync(
        string name,
        bool isPrivate = false,
        string? password = null
    )
    {
        var response = await Client.PostAsJsonAsync(
            "/api/rooms",
            new CreateRoomDto(name, isPrivate, password));

        return await ReadSuccessAsync<RoomDetailsDto>(response);
    }

    private static Message CreateMessage(
        Guid id,
        Guid roomId,
        Guid senderUserId,
        string content,
        DateTimeOffset createdAt
    )
    {
        return Message.Create(id, roomId, senderUserId, content, createdAt);
    }

    private static async Task<T> ReadSuccessAsync<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.IsSuccessStatusCode, content);

        return JsonSerializer.Deserialize<T>(content, JsonOptions)
            ?? throw new InvalidOperationException("Response body was empty.");
    }

    private static async Task<JsonElement> ReadProblemAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode
    )
    {
        var content = await response.Content.ReadAsStringAsync();
        Assert.Equal(expectedStatusCode, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = JsonDocument.Parse(content);

        return document.RootElement.Clone();
    }
}
