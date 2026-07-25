using DistributedChat.Domain.Messages;
using DistributedChat.Domain.ProcessedEvents;
using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace DistributedChat.Infrastructure.Persistence;

public sealed class DistributedChatDbContext(DbContextOptions<DistributedChatDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Room> Rooms => Set<Room>();

    public DbSet<RoomMember> RoomMembers => Set<RoomMember>();

    public DbSet<UserPresence> UserPresences => Set<UserPresence>();

    public DbSet<Message> Messages => Set<Message>();

    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        NormalizeDateTimeOffsetsToUtc();

        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default
    )
    {
        NormalizeDateTimeOffsetsToUtc();

        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DistributedChatDbContext).Assembly);
    }

    private void NormalizeDateTimeOffsetsToUtc()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified);

        foreach (var property in entries.SelectMany(entry => entry.Properties))
        {
            if (property.Metadata.ClrType == typeof(DateTimeOffset)
                && property.CurrentValue is DateTimeOffset value)
            {
                property.CurrentValue = value.ToUniversalTime();
            }
        }
    }
}
