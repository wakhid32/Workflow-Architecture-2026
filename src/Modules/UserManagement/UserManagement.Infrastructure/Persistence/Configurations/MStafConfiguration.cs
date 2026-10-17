using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Konfigurasi EF Core untuk entitas M_STAF (Master Karyawan).
/// Mengimplementasikan tabel, kolom, indeks, dan constraint sesuai DDL di SECURITY.md Section 5.1.
/// Termasuk Filtered Unique Index untuk strict 1-to-1 Keycloak linking.
/// </summary>
public sealed class MStafConfiguration : IEntityTypeConfiguration<MStaf>
{
    public void Configure(EntityTypeBuilder<MStaf> builder)
    {
        builder.ToTable("M_STAF");
        builder.HasKey(e => e.IdStaf);

        builder.Property(e => e.IdStaf)
            .HasColumnName("ID_STAF")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.Nip)
            .HasColumnName("NIP")
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.NamaLengkap)
            .HasColumnName("NAMA_LENGKAP")
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(e => e.Email)
            .HasColumnName("EMAIL")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.NomorHp)
            .HasColumnName("NOMOR_HP")
            .HasMaxLength(25);

        builder.Property(e => e.KodeDepartemen)
            .HasColumnName("KODE_DEPARTEMEN")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.KodeJabatan)
            .HasColumnName("KODE_JABATAN")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IdAtasanLangsung)
            .HasColumnName("ID_ATASAN_LANGSUNG");

        builder.Property(e => e.KeycloakUserId)
            .HasColumnName("KEYCLOAK_USER_ID")
            .HasMaxLength(64);

        builder.Property(e => e.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(e => e.CreatedDate)
            .HasColumnName("CREATED_DATE")
            .IsRequired();

        builder.Property(e => e.CreatedBy)
            .HasColumnName("CREATED_BY")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.UpdatedDate)
            .HasColumnName("UPDATED_DATE")
            .IsRequired();

        builder.Property(e => e.UpdatedBy)
            .HasColumnName("UPDATED_BY")
            .HasMaxLength(50)
            .IsRequired();

        // ─── Unique Constraints ──────────────────────────────────────────────

        builder.HasIndex(e => e.Nip)
            .IsUnique()
            .HasDatabaseName("UQ_M_STAF_NIP");

        builder.HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("UQ_M_STAF_EMAIL");

        // STRICT 1-TO-1 ACTIVE USER CONSTRAINT:
        // 1 User Keycloak hanya boleh terhubung ke 1 Karyawan Aktif
        builder.HasIndex(e => e.KeycloakUserId)
            .IsUnique()
            .HasFilter("\"IS_ACTIVE\" = TRUE AND \"KEYCLOAK_USER_ID\" IS NOT NULL")
            .HasDatabaseName("UQ_M_STAF_KEYCLOAK_ACTIVE");

        // ─── Performance Indexes ─────────────────────────────────────────────

        builder.HasIndex(e => new { e.KodeDepartemen, e.KodeJabatan })
            .HasDatabaseName("IX_M_STAF_DEPT_JABATAN");

        builder.HasIndex(e => e.IdAtasanLangsung)
            .HasDatabaseName("IX_M_STAF_ATASAN");

        // ─── Self-Referencing Hierarchy ──────────────────────────────────────

        builder.HasOne(e => e.AtasanLangsung)
            .WithMany(e => e.Bawahan)
            .HasForeignKey(e => e.IdAtasanLangsung)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
