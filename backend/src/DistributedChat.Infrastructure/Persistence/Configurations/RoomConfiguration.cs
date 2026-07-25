using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");

        builder.HasKey(room => room.Id).HasName("pk_rooms");

        builder.Property(room => room.Id).HasColumnName("id").ValueGeneratedNever();

        builder
            .Property(room => room.Name)
            .HasColumnName("name")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(room => room.CreatedByUserId).HasColumnName("created_by_user_id").IsRequired();

        builder
            .Property(room => room.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(room => room.CreatedAt).HasDatabaseName("ix_rooms_created_at");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(room => room.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_rooms_users_created_by_user_id");
    }
}
