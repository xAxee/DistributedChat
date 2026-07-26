using DistributedChat.Domain.Messages;
using DistributedChat.Domain.Rooms;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages");

        builder.HasKey(message => message.Id).HasName("pk_messages");

        builder.Property(message => message.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(message => message.RoomId).HasColumnName("room_id").IsRequired();

        builder.Property(message => message.SenderUserId).HasColumnName("sender_user_id").IsRequired();

        builder
            .Property(message => message.Content)
            .HasColumnName("content")
            .HasMaxLength(Message.MaximumContentLength)
            .IsRequired();

        builder
            .Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(message => new { message.RoomId, message.CreatedAt, message.Id })
            .HasDatabaseName("ix_messages_room_id_created_at_id");

        builder
            .HasOne<Room>()
            .WithMany()
            .HasForeignKey(message => message.RoomId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("fk_messages_rooms_room_id");

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(message => message.SenderUserId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_messages_users_sender_user_id");
    }
}
