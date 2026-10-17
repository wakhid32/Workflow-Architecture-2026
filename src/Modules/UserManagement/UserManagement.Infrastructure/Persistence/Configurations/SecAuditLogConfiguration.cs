using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence.Configurations;

/// <summary>
/// Konfigurasi EF Core untuk entitas SEC_AUDIT_LOG (Audit Forensik Keamanan).
/// Menyimpan rekam jejak setiap aksi autentikasi dan manajemen staf
/// sesuai DDL di SECURITY.md Section 5.1.
/// </summary>
public sealed class SecAuditLogConfiguration : IEntityTypeConfiguration<SecAuditLog>
{
    public void Configure(EntityTypeBuilder<SecAuditLog> builder)
    {
        builder.ToTable("SEC_AUDIT_LOG");
        builder.HasKey(e => e.IdLog);

        builder.Property(e => e.IdLog)
            .HasColumnName("ID_LOG")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.CorrelationId)
            .HasColumnName("CORRELATION_ID")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.ActionName)
            .HasColumnName("ACTION_NAME")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ActorUserId)
            .HasColumnName("ACTOR_USER_ID")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.ActorStaffId)
            .HasColumnName("ACTOR_STAFF_ID");

        builder.Property(e => e.ResourceName)
            .HasColumnName("RESOURCE_NAME")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ResourceId)
            .HasColumnName("RESOURCE_ID")
            .HasMaxLength(100);

        builder.Property(e => e.Status)
            .HasColumnName("STATUS")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(e => e.ClientIp)
            .HasColumnName("CLIENT_IP")
            .HasMaxLength(45)
            .IsRequired();

        builder.Property(e => e.UserAgent)
            .HasColumnName("USER_AGENT")
            .HasMaxLength(255);

        builder.Property(e => e.PayloadSnapshot)
            .HasColumnName("PAYLOAD_SNAPSHOT")
            .HasColumnType("jsonb");

        builder.Property(e => e.Timestamp)
            .HasColumnName("TIMESTAMP")
            .IsRequired();

        // ─── Performance Indexes ─────────────────────────────────────────────

        builder.HasIndex(e => e.CorrelationId)
            .HasDatabaseName("IX_SEC_AUDIT_CORRELATION");

        builder.HasIndex(e => e.Timestamp)
            .IsDescending()
            .HasDatabaseName("IX_SEC_AUDIT_TIMESTAMP");

        builder.HasIndex(e => e.ActorStaffId)
            .HasDatabaseName("IX_SEC_AUDIT_STAFF");
    }
}
