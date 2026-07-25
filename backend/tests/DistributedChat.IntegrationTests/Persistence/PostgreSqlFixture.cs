using DistributedChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace DistributedChat.IntegrationTests.Persistence;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.1-alpine")
        .WithDatabase("distributed_chat_tests")
        .WithUsername("distributed_chat")
        .WithPassword("distributed_chat_test_password")
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        await using var dbContext = CreateDbContext();
        await dbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public DistributedChatDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<DistributedChatDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        return new DistributedChatDbContext(options);
    }

    public async Task ResetDatabaseAsync()
    {
        await using var dbContext = CreateDbContext();

        await dbContext.Database.ExecuteSqlRawAsync(
            """
            TRUNCATE TABLE
                messages,
                room_members,
                user_presences,
                rooms,
                users,
                processed_events;
            """
        );
    }
}
