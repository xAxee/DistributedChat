using DistributedChat.Domain.ProcessedEvents;
using DistributedChat.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DistributedChat.Infrastructure.Persistence.Configurations;

public sealed class ProcessedEventConfiguration : IEntityTypeConfiguration<ProcessedEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedEvent> builder)
    {
        builder.ToTable("processed_events");

        builder
            .HasKey(processedEvent => new { processedEvent.ConsumerId, processedEvent.EventId })
            .HasName("pk_processed_events");

        builder
            .Property(processedEvent => processedEvent.ConsumerId)
            .HasColumnName("consumer_id")
            .HasMaxLength(UserPresence.MaximumInstanceIdLength)
            .IsRequired();

        builder.Property(processedEvent => processedEvent.EventId).HasColumnName("event_id").ValueGeneratedNever();

        builder
            .Property(processedEvent => processedEvent.ProcessedAt)
            .HasColumnName("processed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder
            .HasIndex(processedEvent => processedEvent.ProcessedAt)
            .HasDatabaseName("ix_processed_events_processed_at");
    }
}
