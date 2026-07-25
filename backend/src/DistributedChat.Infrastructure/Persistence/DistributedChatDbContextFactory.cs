using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DistributedChat.Infrastructure.Persistence;

public sealed class DistributedChatDbContextFactory : IDesignTimeDbContextFactory<DistributedChatDbContext>
{
    private const string ConnectionStringEnvironmentVariable = "ConnectionStrings__DistributedChat";

    private const string ConnectionStringArgumentPrefix = "--connection-string=";

    private const string LocalDevelopmentConnectionString =
        "Host=localhost;Port=5432;Database=distributed_chat;Username=distributed_chat;Password=distributed_chat_local_password";

    public DistributedChatDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<DistributedChatDbContext>();

        optionsBuilder.UseNpgsql(
            GetConnectionString(args),
            npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(DistributedChatDbContext).Assembly.FullName);
            }
        );

        return new DistributedChatDbContext(optionsBuilder.Options);
    }

    private static string GetConnectionString(IEnumerable<string> args)
    {
        foreach (var argument in args)
        {
            if (argument.StartsWith(ConnectionStringArgumentPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = argument[ConnectionStringArgumentPrefix.Length..];

                if (!string.IsNullOrWhiteSpace(connectionString))
                {
                    return connectionString;
                }
            }
        }

        return Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
            ?? LocalDevelopmentConnectionString;
    }
}
