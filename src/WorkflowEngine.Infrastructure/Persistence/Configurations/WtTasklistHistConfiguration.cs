using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WtTasklistHistConfiguration : IEntityTypeConfiguration<WtTasklistHist>
{
    public void Configure(EntityTypeBuilder<WtTasklistHist> builder)
    {
        builder.ToTable("WT_TASKLIST_HIST");
        builder.HasKey(e => e.IdTaskHist);

        builder.Property(e => e.IdTaskHist)
            .HasColumnName("ID_TASK_HIST")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdTask)
            .HasColumnName("ID_TASK")
            .IsRequired();

        builder.Property(e => e.IdUrutanDisp)
            .HasColumnName("ID_URUTAN_DISP")
            .IsRequired();

        builder.Property(e => e.IdUserAsal)
            .HasColumnName("ID_USER_ASAL")
            .IsRequired();

        builder.Property(e => e.IdUserTujuan)
            .HasColumnName("ID_USER_TUJUAN")
            .IsRequired();

        builder.Property(e => e.IdUserEksekutor)
            .HasColumnName("ID_USER_EKSEKUTOR")
            .IsRequired();

        builder.Property(e => e.IdStatusDok)
            .HasColumnName("ID_STATUS_DOK")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.ActionTaken)
            .HasColumnName("ACTION_TAKEN")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.TglTaskMulai)
            .HasColumnName("TGL_TASK_MULAI")
            .IsRequired();

        builder.Property(e => e.TglTaskAkhir)
            .HasColumnName("TGL_TASK_AKHIR")
            .IsRequired();

        builder.Property(e => e.DurationMinutes)
            .HasColumnName("DURATION_MINUTES")
            .HasDefaultValue(0);

        builder.Property(e => e.TaskInstanceJson)
            .HasColumnName("TASK_INSTANCE_JSON");

        builder.Property(e => e.TotalAttachments)
            .HasColumnName("TOTAL_ATTACHMENTS")
            .HasDefaultValue(0);

        builder.Property(e => e.ClientIp)
            .HasColumnName("CLIENT_IP")
            .HasMaxLength(50);

        builder.Property(e => e.UserAgent)
            .HasColumnName("USER_AGENT")
            .HasMaxLength(255);

        builder.HasIndex(e => new { e.IdTask, e.TglTaskAkhir })
            .HasDatabaseName("IX_HIST_TASK_AUDIT");
    }
}
