using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WtTasklistAssigneeConfiguration : IEntityTypeConfiguration<WtTasklistAssignee>
{
    public void Configure(EntityTypeBuilder<WtTasklistAssignee> builder)
    {
        builder.ToTable("WT_TASKLIST_ASSIGNEE");
        builder.HasKey(e => e.IdAssignee);

        builder.Property(e => e.IdAssignee)
            .HasColumnName("ID_ASSIGNEE")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdTask)
            .HasColumnName("ID_TASK")
            .IsRequired();

        builder.Property(e => e.IdUrutanDisp)
            .HasColumnName("ID_URUTAN_DISP")
            .IsRequired();

        builder.Property(e => e.IdUser)
            .HasColumnName("ID_USER")
            .IsRequired();

        builder.Property(e => e.StatusApproval)
            .HasColumnName("STATUS_APPROVAL")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(AssigneeApprovalStatus.Pending)
            .IsRequired();

        builder.Property(e => e.TglRespon)
            .HasColumnName("TGL_RESPON");

        builder.Property(e => e.Catatan)
            .HasColumnName("CATATAN")
            .HasMaxLength(500);

        builder.Property(e => e.IsDelegated)
            .HasColumnName("IS_DELEGATED")
            .HasDefaultValue(false);

        builder.Property(e => e.IdDelegatedFrom)
            .HasColumnName("ID_DELEGATED_FROM");

        builder.HasIndex(e => new { e.IdTask, e.IdUser, e.StatusApproval })
            .HasDatabaseName("IX_ASSIGNEE_TASK_USER");
    }
}
