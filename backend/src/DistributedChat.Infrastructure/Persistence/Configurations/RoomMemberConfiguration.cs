using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class RoomMemberConfiguration : IEntityTypeConfiguration<RoomMember>
{
    public void Configure(EntityTypeBuilder<RoomMember> builder)
    {
        builder.ToTable("room_members");

        builder.HasKey(roomMember => new { roomMember.RoomId, roomMember.UserId }).HasName("pk_room_members");

        builder.Property(roomMember => roomMember.RoomId).HasColumnName("room_id").ValueGeneratedNever();

        builder.Property(roomMember => roomMember.UserId).HasColumnName("user_id").ValueGeneratedNever();

        builder
            .Property(roomMember => roomMember.JoinedAt)
            .HasColumnName("joined_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(roomMember => roomMember.UserId).HasDatabaseName("ix_room_members_user_id");

        builder
            .HasOne<Room>()
            .WithMany()
            .HasForeignKey(roomMember => roomMember.RoomId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_room_members_rooms_room_id");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(roomMember => roomMember.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_room_members_users_user_id");
    }
}
