using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WmWorkflowDelegationConfiguration : IEntityTypeConfiguration<WmWorkflowDelegation>
{
    public void Configure(EntityTypeBuilder<WmWorkflowDelegation> builder)
    {
        builder.ToTable("WM_WORKFLOW_DELEGATION");
        builder.HasKey(e => e.IdDelegation);

        builder.Property(e => e.IdDelegation)
            .HasColumnName("ID_DELEGATION")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdUserPemberi)
            .HasColumnName("ID_USER_PEMBERI")
            .IsRequired();

        builder.Property(e => e.IdUserPenerima)
            .HasColumnName("ID_USER_PENERIMA")
            .IsRequired();

        builder.Property(e => e.IdTipeDisposisi)
            .HasColumnName("ID_TIPE_DISPOSISI");

        builder.Property(e => e.TanggalMulai)
            .HasColumnName("TANGGAL_MULAI")
            .IsRequired();

        builder.Property(e => e.TanggalSelesai)
            .HasColumnName("TANGGAL_SELESAI")
            .IsRequired();

        builder.Property(e => e.AlasanDelegasi)
            .HasColumnName("ALASAN_DELEGASI")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedDate)
            .HasColumnName("CREATED_DATE")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("CREATED_BY")
            .IsRequired();

        builder.HasOne(e => e.TipeDisposisi)
            .WithMany()
            .HasForeignKey(e => e.IdTipeDisposisi)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.IdUserPemberi, e.IsActive, e.TanggalMulai, e.TanggalSelesai })
            .HasDatabaseName("IX_DELEGATION_ACTIVE");
    }
}
