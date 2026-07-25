using System.Net;
using System.Text.Json;
using DistributedChat.Api.Http;
using DistributedChat.IntegrationTests.Persistence;

namespace DistributedChat.IntegrationTests.Api;

[Collection(TestCollections.PostgreSql)]
public sealed class StatusEndpointTests(PostgreSqlFixture fixture) : IAsyncLifetime, IDisposable
{
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
    public async Task HealthAndStatusEndpointsReturnObservabilityMetadata()
    {
        var live = await Client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, live.StatusCode);
        Assert.Equal("test-api", live.Headers.GetValues(InstanceHeaderMiddleware.HeaderName).Single());

        var ready = await Client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, ready.StatusCode);

        var statusResponse = await Client.GetAsync("/api/status");
        Assert.Equal(HttpStatusCode.OK, statusResponse.StatusCode);
        Assert.Equal("test-api", statusResponse.Headers.GetValues(InstanceHeaderMiddleware.HeaderName).Single());

        using var document = JsonDocument.Parse(await statusResponse.Content.ReadAsStringAsync());
        var status = document.RootElement;

        Assert.Equal("test-api", status.GetProperty("instanceId").GetString());
        Assert.Equal(0, status.GetProperty("activeConnections").GetInt32());
        Assert.Equal(0, status.GetProperty("connectedUsers").GetInt32());
        Assert.False(status.TryGetProperty("rabbitMqConnected", out _));
        Assert.False(status.TryGetProperty("databaseConnected", out _));
        Assert.True(status.GetProperty("uptimeSeconds").GetInt64() >= 0);
        Assert.False(string.IsNullOrWhiteSpace(status.GetProperty("applicationVersion").GetString()));
    }

    [Fact]
    public async Task CorrelationIdMiddlewareAcceptsValidGuidAndGeneratesForInvalidHeader()
    {
        var correlationId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        using var validRequest = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        validRequest.Headers.Add(CorrelationContext.CorrelationIdHeaderName, correlationId.ToString("D"));

        var validResponse = await Client.SendAsync(validRequest);

        Assert.Equal(
            correlationId.ToString("D"),
            validResponse.Headers.GetValues(CorrelationContext.CorrelationIdHeaderName).Single());

        using var invalidRequest = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        invalidRequest.Headers.Add(CorrelationContext.CorrelationIdHeaderName, "not-a-guid");

        var invalidResponse = await Client.SendAsync(invalidRequest);
        var generated = invalidResponse.Headers.GetValues(CorrelationContext.CorrelationIdHeaderName).Single();

        Assert.True(Guid.TryParse(generated, out var generatedCorrelationId));
        Assert.NotEqual(Guid.Empty, generatedCorrelationId);
    }

    private HttpClient Client => client ?? throw new InvalidOperationException("Test client is not initialized.");
}
