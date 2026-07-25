using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using DistributedChat.Api.Dtos;
using DistributedChat.Application.Common.Dto;
using DistributedChat.IntegrationTests.Persistence;

namespace DistributedChat.IntegrationTests.Api;

[Collection(TestCollections.PostgreSql)]
public sealed class AuthEndpointTests(PostgreSqlFixture fixture) : IAsyncLifetime, IDisposable
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
    public async Task RegisterReturnsAuthResponse()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto("  Alice@Example.com  ", "  Alice  ", "password123"));

        var auth = await ReadSuccessAsync<AuthResponse>(response);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.True(auth.ExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal("Alice", auth.User.Username);
        Assert.Equal("Alice@Example.com", auth.User.Email);
    }

    [Fact]
    public async Task RegisterReturnsConflictForExistingUsername()
    {
        await RegisterAsync("alice@example.com", "alice");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto("alice2@example.com", "Alice", "password123"));

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);

        Assert.Equal("Conflict", problem.GetProperty("title").GetString());
        Assert.Equal(409, problem.GetProperty("status").GetInt32());
        Assert.Equal("Username is already in use.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task RegisterReturnsConflictForExistingEmail()
    {
        await RegisterAsync("alice@example.com", "alice");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto("Alice@Example.com", "alice2", "password123"));

        var problem = await ReadProblemAsync(response, HttpStatusCode.Conflict);

        Assert.Equal("Email is already in use.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task LoginWorksWithUsername()
    {
        await RegisterAsync("alice@example.com", "alice");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto("alice", "password123"));

        var auth = await ReadSuccessAsync<AuthResponse>(response);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("alice", auth.User.Username);
    }

    [Fact]
    public async Task LoginWorksWithEmail()
    {
        await RegisterAsync("alice@example.com", "alice");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto("Alice@Example.com", "password123"));

        var auth = await ReadSuccessAsync<AuthResponse>(response);

        Assert.False(string.IsNullOrWhiteSpace(auth.AccessToken));
        Assert.Equal("alice@example.com", auth.User.Email);
    }

    [Fact]
    public async Task LoginReturnsSameMessageForWrongPassword()
    {
        await RegisterAsync("alice@example.com", "alice");

        var response = await Client.PostAsJsonAsync(
            "/api/auth/login",
            new LoginDto("alice", "wrong-password"));

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);

        Assert.Equal("Invalid login or password.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UsersMeReturnsCurrentUserWithToken()
    {
        var auth = await RegisterAsync("alice@example.com", "alice");
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);

        var response = await Client.GetAsync("/api/users/me");

        var user = await ReadSuccessAsync<CurrentUserDto>(response);
        Assert.Equal(auth.User.Id, user.Id);
        Assert.Equal("alice", user.Username);
        Assert.Equal("alice@example.com", user.Email);
    }

    [Fact]
    public async Task UsersMeReturnsUnauthorizedWithoutToken()
    {
        var response = await Client.GetAsync("/api/users/me");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Authentication is required.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UsersMeReturnsUnauthorizedWithInvalidToken()
    {
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "not-a-valid-token");

        var response = await Client.GetAsync("/api/users/me");

        var problem = await ReadProblemAsync(response, HttpStatusCode.Unauthorized);
        Assert.Equal("Authentication is required.", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task ValidationErrorsUseProblemDetailsWithFieldErrors()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto("invalid", "ab", "short"));

        var problem = await ReadProblemAsync(response, HttpStatusCode.BadRequest);

        Assert.Equal("Bad Request", problem.GetProperty("title").GetString());
        Assert.Equal(400, problem.GetProperty("status").GetInt32());
        Assert.True(problem.GetProperty("errors").TryGetProperty("Email", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("Username", out _));
        Assert.True(problem.GetProperty("errors").TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task RegisterRateLimitReturnsProblemDetails()
    {
        for (var i = 0; i < 5; i++)
        {
            var response = await Client.PostAsJsonAsync(
                "/api/auth/register",
                new RegisterDto("invalid", "ab", "short"));

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        var rejected = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto("invalid", "ab", "short"));

        var problem = await ReadProblemAsync(rejected, HttpStatusCode.TooManyRequests);
        Assert.Equal("Too Many Requests", problem.GetProperty("title").GetString());
        Assert.Equal("Too many requests. Please try again later.", problem.GetProperty("detail").GetString());
    }

    private HttpClient Client => client ?? throw new InvalidOperationException("Test client is not initialized.");

    private async Task<AuthResponse> RegisterAsync(string email, string username)
    {
        var response = await Client.PostAsJsonAsync(
            "/api/auth/register",
            new RegisterDto(email, username, "password123"));

        return await ReadSuccessAsync<AuthResponse>(response);
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
