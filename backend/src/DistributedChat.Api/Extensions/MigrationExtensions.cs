using DistributedChat.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Api.Extensions;

public static class MigrationExtensions
{
    public static async Task<bool> ApplyDatabaseMigrationsIfRequestedAsync(
        this WebApplication app,
        IEnumerable<string> args
    )
    {
        var isMigrationCommand = IsMigrationCommand(args);
        if (!isMigrationCommand && !app.Environment.IsDevelopment())
        {
            return false;
        }

        await using var scope = app.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DistributedChatDbContext>();

        await dbContext.Database.MigrateAsync();
        await DistributedChatDbSeeder.SeedGlobalRoomAsync(dbContext);

        return isMigrationCommand;
    }

    private static bool IsMigrationCommand(IEnumerable<string> args)
    {
        return args.Any(arg =>
            string.Equals(arg, "--migrate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "migrate", StringComparison.OrdinalIgnoreCase)
            || string.Equals(arg, "database-update", StringComparison.OrdinalIgnoreCase));
    }
}
