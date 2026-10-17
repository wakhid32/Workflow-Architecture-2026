using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WmTipeDisposisiConfiguration : IEntityTypeConfiguration<WmTipeDisposisi>
{
    public void Configure(EntityTypeBuilder<WmTipeDisposisi> builder)
    {
        builder.ToTable("WM_TIPE_DISPOSISI");
        builder.HasKey(e => e.IdTipeDisposisi);

        builder.Property(e => e.IdTipeDisposisi)
            .HasColumnName("ID_TIPE_DISPOSISI")
            .ValueGeneratedNever();

        builder.Property(e => e.NamaDisposisi)
            .HasColumnName("NAMA_DISPOSISI")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.Deskripsi)
            .HasColumnName("DESKRIPSI")
            .HasMaxLength(255);

        builder.Property(e => e.KodeModul)
            .HasColumnName("KODE_MODUL")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasDefaultValue(true);

        builder.Property(e => e.IsReopenClosed)
            .HasColumnName("IS_REOPEN_CLOSED")
            .HasDefaultValue(false);

        builder.Property(e => e.CreatedDate)
            .HasColumnName("CREATED_DATE")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("CREATED_BY")
            .HasMaxLength(50);

        builder.HasIndex(e => e.KodeModul)
            .HasDatabaseName("IX_WM_TIPE_DISPOSISI_KODEMODUL");
    }
}
