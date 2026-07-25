using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class UserPresenceConfiguration : IEntityTypeConfiguration<UserPresence>
{
    public void Configure(EntityTypeBuilder<UserPresence> builder)
    {
        builder.ToTable("user_presences");

        builder.HasKey(presence => new { presence.UserId, presence.InstanceId })
            .HasName("pk_user_presences");

        builder.Property(presence => presence.UserId).HasColumnName("user_id").ValueGeneratedNever();

        builder
            .Property(presence => presence.InstanceId)
            .HasColumnName("instance_id")
            .HasMaxLength(UserPresence.MaximumInstanceIdLength)
            .IsRequired();

        builder
            .Property(presence => presence.ConnectionCount)
            .HasColumnName("connection_count")
            .IsRequired();

        builder
            .Property(presence => presence.LastHeartbeatAt)
            .HasColumnName("last_heartbeat_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .Property(presence => presence.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(presence => new { presence.UserId, presence.ConnectionCount, presence.LastHeartbeatAt })
            .HasDatabaseName("ix_user_presences_user_id_active_heartbeat");

        builder
            .HasIndex(presence => presence.InstanceId)
            .HasDatabaseName("ix_user_presences_instance_id");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(presence => presence.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_user_presences_users_user_id");
    }
}
