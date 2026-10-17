using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Infrastructure.Persistence.Configurations;

public sealed class WmUrutanDisposisiConfiguration : IEntityTypeConfiguration<WmUrutanDisposisi>
{
    public void Configure(EntityTypeBuilder<WmUrutanDisposisi> builder)
    {
        builder.ToTable("WM_URUTAN_DISPOSISI");
        builder.HasKey(e => e.IdUrutanDisp);

        builder.Property(e => e.IdUrutanDisp)
            .HasColumnName("ID_URUTAN_DISP")
            .ValueGeneratedOnAdd();

        builder.Property(e => e.IdTipeDisposisi)
            .HasColumnName("ID_TIPE_DISPOSISI")
            .IsRequired();

        builder.Property(e => e.IdRoleAsal)
            .HasColumnName("ID_ROLE_ASAL")
            .IsRequired();

        builder.Property(e => e.IdStatusDokAsal)
            .HasColumnName("ID_STATUS_DOK_ASAL")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IdRole)
            .HasColumnName("ID_ROLE")
            .IsRequired();

        builder.Property(e => e.IdStatusDok)
            .HasColumnName("ID_STATUS_DOK")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(e => e.IdForm)
            .HasColumnName("ID_FORM")
            .IsRequired();

        builder.Property(e => e.KodeDepartemen)
            .HasColumnName("KODE_DEPARTEMEN")
            .HasMaxLength(50);

        builder.Property(e => e.KodeJabatan)
            .HasColumnName("KODE_JABATAN")
            .HasMaxLength(50);

        builder.Property(e => e.Nama)
            .HasColumnName("NAMA")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.ClassButton)
            .HasColumnName("CLASS_BUTTON")
            .HasMaxLength(50)
            .HasDefaultValue("btn-primary");

        builder.Property(e => e.ClassIcon)
            .HasColumnName("CLASS_ICON")
            .HasMaxLength(50)
            .HasDefaultValue("fa fa-check");

        builder.Property(e => e.ActionType)
            .HasColumnName("ACTION_TYPE")
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(e => e.ApprovalMode)
            .HasColumnName("APPROVAL_MODE")
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.ApprovalMode.Single);

        builder.Property(e => e.UiFormAction)
            .HasColumnName("UI_FORM_ACTION")
            .HasMaxLength(100);

        builder.Property(e => e.UiFormSeting)
            .HasColumnName("UI_FORM_SETING")
            .HasMaxLength(50)
            .HasDefaultValue("readonly");

        builder.Property(e => e.IsAwal)
            .HasColumnName("IS_AWAL")
            .HasDefaultValue(false);

        builder.Property(e => e.IsAkhir)
            .HasColumnName("IS_AKHIR")
            .HasDefaultValue(false);

        builder.Property(e => e.IsKirimAtasan)
            .HasColumnName("IS_KIRIM_ATASAN")
            .HasDefaultValue(false);

        builder.Property(e => e.IsKirimPembuat)
            .HasColumnName("IS_KIRIM_PEMBUAT")
            .HasDefaultValue(false);

        builder.Property(e => e.IsKirimSebelumnya)
            .HasColumnName("IS_KIRIM_SEBELUMNYA")
            .HasDefaultValue(false);

        builder.Property(e => e.IsRequireAttachment)
            .HasColumnName("IS_REQUIRE_ATTACHMENT")
            .HasDefaultValue(false);

        builder.Property(e => e.SlaHoursDefault)
            .HasColumnName("SLA_HOURS_DEFAULT");

        builder.Property(e => e.AmtLimitMin)
            .HasColumnName("AMT_LIMIT_MIN")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.AmtLimitMax)
            .HasColumnName("AMT_LIMIT_MAX")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(e => e.ConditionExpressionJson)
            .HasColumnName("CONDITION_EXPRESSION_JSON");

        builder.Property(e => e.NoUrutan)
            .HasColumnName("NO_URUTAN")
            .HasDefaultValue(1);

        builder.Property(e => e.IsActive)
            .HasColumnName("IS_ACTIVE")
            .HasDefaultValue(true);

        builder.HasOne(e => e.TipeDisposisi)
            .WithMany(t => t.UrutanDisposisiList)
            .HasForeignKey(e => e.IdTipeDisposisi)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.IdTipeDisposisi, e.IdStatusDokAsal, e.IsActive })
            .HasDatabaseName("IX_URUTAN_STATE_LOOKUP");
    }
}
