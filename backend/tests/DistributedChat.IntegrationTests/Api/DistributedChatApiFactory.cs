using DistributedChat.IntegrationTests.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;

namespace DistributedChat.IntegrationTests.Api;

public sealed class DistributedChatApiFactory(PostgreSqlFixture fixture) : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__DistributedChat", fixture.ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "DistributedChat.Tests");
        Environment.SetEnvironmentVariable("Jwt__Audience", "DistributedChat.Tests.Api");
        Environment.SetEnvironmentVariable(
            "Jwt__SigningKey",
            "integration-test-signing-key-with-at-least-32-characters");
        Environment.SetEnvironmentVariable("Instance__InstanceId", "test-api");
        Environment.SetEnvironmentVariable("Jwt__ExpirationMinutes", "60");
        Environment.SetEnvironmentVariable("Messaging__Transport", "InMemory");

        return base.CreateHost(builder);
    }
}
