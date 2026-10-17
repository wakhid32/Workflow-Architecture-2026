using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WtWorkflowOutboxConfiguration : IEntityTypeConfiguration<WtWorkflowOutbox>
{
    public void Configure(EntityTypeBuilder<WtWorkflowOutbox> builder)
    {
        builder.ToTable("WT_WORKFLOW_OUTBOX");
        builder.HasKey(e => e.IdOutbox);

        builder.Property(e => e.IdOutbox)
            .HasColumnName("ID_OUTBOX")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdTask)
            .HasColumnName("ID_TASK")
            .IsRequired();

        builder.Property(e => e.EventType)
            .HasColumnName("EVENT_TYPE")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.RecipientPayloadJson)
            .HasColumnName("RECIPIENT_PAYLOAD_JSON")
            .IsRequired();

        builder.Property(e => e.MessagePayloadJson)
            .HasColumnName("MESSAGE_PAYLOAD_JSON")
            .IsRequired();

        builder.Property(e => e.Status)
            .HasColumnName("STATUS")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(OutboxStatus.Pending)
            .IsRequired();

        builder.Property(e => e.RetryCount)
            .HasColumnName("RETRY_COUNT")
            .HasDefaultValue(0);

        builder.Property(e => e.CreatedDate)
            .HasColumnName("CREATED_DATE")
            .IsRequired();

        builder.Property(e => e.ProcessedDate)
            .HasColumnName("PROCESSED_DATE");

        builder.Property(e => e.LastError)
            .HasColumnName("LAST_ERROR");

        builder.HasIndex(e => new { e.Status, e.CreatedDate })
            .HasDatabaseName("IX_OUTBOX_PENDING");
    }
}
