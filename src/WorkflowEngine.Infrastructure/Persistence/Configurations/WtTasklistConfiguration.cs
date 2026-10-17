using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WtTasklistConfiguration : IEntityTypeConfiguration<WtTasklist>
{
    public void Configure(EntityTypeBuilder<WtTasklist> builder)
    {
        builder.ToTable("WT_TASKLIST");
        builder.HasKey(e => e.IdTask);

        builder.Property(e => e.IdTask)
            .HasColumnName("ID_TASK")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.HashId)
            .HasColumnName("HASH_ID")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.IdTipeDisposisi)
            .HasColumnName("ID_TIPE_DISPOSISI")
            .IsRequired();

        builder.Property(e => e.IdUrutanDisp)
            .HasColumnName("ID_URUTAN_DISP")
            .IsRequired();

        builder.Property(e => e.IdDokumen)
            .HasColumnName("ID_DOKUMEN")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.NoDokumen)
            .HasColumnName("NO_DOKUMEN")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.IdForm)
            .HasColumnName("ID_FORM")
            .IsRequired();

        builder.Property(e => e.IdUserAsal)
            .HasColumnName("ID_USER_ASAL")
            .IsRequired();

        builder.Property(e => e.IdUserTujuan)
            .HasColumnName("ID_USER_TUJUAN")
            .IsRequired();

        builder.Property(e => e.PrevIdUser)
            .HasColumnName("PREV_ID_USER");

        builder.Property(e => e.IdStatusDok)
            .HasColumnName("ID_STATUS_DOK")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ApprovalStatus)
            .HasColumnName("APPROVAL_STATUS")
            .HasConversion<string>()
            .HasMaxLength(30)
            .HasDefaultValue(ApprovalStatus.InProgress)
            .IsRequired();

        builder.Property(e => e.Priority)
            .HasColumnName("PRIORITY")
            .HasConversion<byte>();

        builder.Property(e => e.TglDispMulai)
            .HasColumnName("TGL_DISP_MULAI")
            .IsRequired();

        builder.Property(e => e.TglDispAkhir)
            .HasColumnName("TGL_DISP_AKHIR");

        builder.Property(e => e.DueDate)
            .HasColumnName("DUE_DATE");

        builder.Property(e => e.SlaHours)
            .HasColumnName("SLA_HOURS");

        builder.Property(e => e.IsOverdue)
            .HasColumnName("IS_OVERDUE")
            .HasDefaultValue(false);

        builder.Property(e => e.TaskInstanceJson)
            .HasColumnName("TASK_INSTANCE_JSON");

        // Optimistic Concurrency Token
        builder.Property(e => e.RowVersion)
            .HasColumnName("ROW_VERSION")
            .IsConcurrencyToken();

        builder.Property(e => e.CreatedDate)
            .HasColumnName("CREATED_DATE")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("CREATED_BY")
            .IsRequired();

        builder.Property(e => e.UpdatedDate)
            .HasColumnName("UPDATED_DATE")
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("UPDATED_BY")
            .IsRequired();

        // Foreign keys & Navigation
        builder.HasOne(e => e.TipeDisposisi)
            .WithMany(t => t.TasklistList)
            .HasForeignKey(e => e.IdTipeDisposisi)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.UrutanDisposisi)
            .WithMany()
            .HasForeignKey(e => e.IdUrutanDisp)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Histories)
            .WithOne(h => h.Tasklist)
            .HasForeignKey(h => h.IdTask)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Assignees)
            .WithOne(a => a.Tasklist)
            .HasForeignKey(a => a.IdTask)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(att => att.Tasklist)
            .HasForeignKey(att => att.IdTask)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.OutboxMessages)
            .WithOne(o => o.Tasklist)
            .HasForeignKey(o => o.IdTask)
            .OnDelete(DeleteBehavior.Cascade);

        // Performance Indexes
        builder.HasIndex(e => e.HashId)
            .IsUnique()
            .HasDatabaseName("UQ_WT_TASKLIST_HASH_ID");

        builder.HasIndex(e => new { e.IdUserTujuan, e.IdStatusDok })
            .HasDatabaseName("IX_TASKLIST_PIC_STATUS");

        builder.HasIndex(e => new { e.IdDokumen, e.IdTipeDisposisi })
            .HasDatabaseName("IX_TASKLIST_DOC_LOOKUP");

        builder.HasIndex(e => new { e.DueDate, e.IsOverdue })
            .HasDatabaseName("IX_TASKLIST_SLA_DUE");
    }
}
