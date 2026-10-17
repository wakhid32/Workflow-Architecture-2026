using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WtTasklistAttachmentConfiguration : IEntityTypeConfiguration<WtTasklistAttachment>
{
    public void Configure(EntityTypeBuilder<WtTasklistAttachment> builder)
    {
        builder.ToTable("WT_TASKLIST_ATTACHMENT");
        builder.HasKey(e => e.IdAttachment);

        builder.Property(e => e.IdAttachment)
            .HasColumnName("ID_ATTACHMENT")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdTask)
            .HasColumnName("ID_TASK")
            .IsRequired();

        builder.Property(e => e.IdTaskHist)
            .HasColumnName("ID_TASK_HIST");

        builder.Property(e => e.FileName)
            .HasColumnName("FILE_NAME")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.FileExtension)
            .HasColumnName("FILE_EXTENSION")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ContentType)
            .HasColumnName("CONTENT_TYPE")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.FileSizeBytes)
            .HasColumnName("FILE_SIZE_BYTES")
            .IsRequired();

        builder.Property(e => e.StoragePath)
            .HasColumnName("STORAGE_PATH")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(e => e.Caption)
            .HasColumnName("CAPTION")
            .HasMaxLength(255);

        builder.Property(e => e.UploadedDate)
            .HasColumnName("UPLOADED_DATE")
            .IsRequired();

        builder.Property(e => e.UploadedBy)
            .HasColumnName("UPLOADED_BY")
            .IsRequired();

        builder.HasOne(e => e.TasklistHist)
            .WithMany(h => h.Attachments)
            .HasForeignKey(e => e.IdTaskHist)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => e.IdTask)
            .HasDatabaseName("IX_ATT_TASK");

        builder.HasIndex(e => e.IdTaskHist)
            .HasDatabaseName("IX_ATT_HIST");
    }
}
