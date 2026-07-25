using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id).HasName("pk_users");

        builder.Property(user => user.Id).HasColumnName("id").ValueGeneratedNever();

        builder
            .Property(user => user.Username)
            .HasColumnName("username")
            .HasMaxLength(User.MaximumUsernameLength)
            .IsRequired();

        builder
            .Property(user => user.NormalizedUsername)
            .HasColumnName("normalized_username")
            .HasMaxLength(User.MaximumUsernameLength)
            .IsRequired();

        builder
            .Property(user => user.Email)
            .HasColumnName("email")
            .HasMaxLength(User.MaximumEmailLength)
            .IsRequired();

        builder
            .Property(user => user.NormalizedEmail)
            .HasColumnName("normalized_email")
            .HasMaxLength(User.MaximumEmailLength)
            .IsRequired();

        builder
            .Property(user => user.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(user => user.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(user => user.NormalizedUsername)
            .IsUnique()
            .HasDatabaseName("ux_users_normalized_username");

        builder
            .HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName("ux_users_normalized_email");
    }
}
