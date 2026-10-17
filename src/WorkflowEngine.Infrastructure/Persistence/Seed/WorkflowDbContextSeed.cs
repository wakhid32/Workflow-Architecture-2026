using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Infrastructure.Persistence.Seed;

/// <summary>
/// Database Initializer & Master Workflow Seeder.
/// Memastikan konfigurasi alur kerja default (PR Procurement, Capex Asset, Reimbursement)
/// telah tersedia dan siap digunakan saat aplikasi pertama kali dijalankan.
/// </summary>
public static class WorkflowDbContextSeed
{
    public static async Task SeedDefaultWorkflowsAsync(WorkflowDbContext context)
    {
        // 1. Tipe Disposisi
        if (!await context.WmTipeDisposisi.AnyAsync())
        {
            var tipes = new List<WmTipeDisposisi>
            {
                new()
                {
                    IdTipeDisposisi = 101,
                    NamaDisposisi = "Persetujuan Purchase Requisition (PR)",
                    Deskripsi = "Alur persetujuan pengadaan barang operasional dan inventaris kantor",
                    KodeModul = "PROCUREMENT_PR",
                    IsActive = true,
                    IsReopenClosed = false,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new()
                {
                    IdTipeDisposisi = 201,
                    NamaDisposisi = "Pengajuan Capex Asset & Investasi",
                    Deskripsi = "Persetujuan pengadaan barang modal dan aset bernilai tinggi",
                    KodeModul = "ASSET_MGMT",
                    IsActive = true,
                    IsReopenClosed = false,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                },
                new()
                {
                    IdTipeDisposisi = 301,
                    NamaDisposisi = "Klaim Reimbursement Operasional",
                    Deskripsi = "Penggantian biaya operasional dan perjalanan dinas karyawan",
                    KodeModul = "FIN_REIMBURSE",
                    IsActive = true,
                    IsReopenClosed = false,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }
            };

            await context.WmTipeDisposisi.AddRangeAsync(tipes);
            await context.SaveChangesAsync();
        }

        // 2. Master Urutan Disposisi (State Machine Transitions)
        if (!await context.WmUrutanDisposisi.AnyAsync())
        {
            var urutanList = new List<WmUrutanDisposisi>
            {
                // ==========================================
                // MODUL 201: CAPEX ASSET APPROVAL
                // ==========================================
                // Step 1: Inisiator Draft -> Submit ke Atasan
                new()
                {
                    IdTipeDisposisi = 201,
                    IdRoleAsal = 1,
                    IdStatusDokAsal = "CAPEX.DRAFT",
                    IdRole = 2,
                    IdStatusDok = "CAPEX.WAITING_SUPERVISOR",
                    IdForm = 501,
                    Nama = "Ajukan ke Atasan Langsung",
                    ClassButton = "btn-primary",
                    ClassIcon = "fa fa-paper-plane",
                    ActionType = WorkflowActionType.Submit,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = "internalsave",
                    UiFormSeting = "insertable",
                    IsAwal = true,
                    IsAkhir = false,
                    IsKirimAtasan = true,
                    IsKirimPembuat = false,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = 24,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 1,
                    IsActive = true
                },
                // Step 2: Atasan Langsung -> Approve & Teruskan ke Direksi Keuangan
                new()
                {
                    IdTipeDisposisi = 201,
                    IdRoleAsal = 2,
                    IdStatusDokAsal = "CAPEX.WAITING_SUPERVISOR",
                    IdRole = 5,
                    IdStatusDok = "CAPEX.WAITING_FINANCE_DIR",
                    IdForm = 501,
                    Nama = "Setujui & Teruskan ke Direksi Keuangan",
                    ClassButton = "btn-success",
                    ClassIcon = "fa fa-check",
                    ActionType = WorkflowActionType.Approve,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = false,
                    IsKirimAtasan = false,
                    IsKirimPembuat = false,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = 48,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 2,
                    IsActive = true
                },
                // Step 3: Atasan Langsung -> Minta Revisi ke Pembuat
                new()
                {
                    IdTipeDisposisi = 201,
                    IdRoleAsal = 2,
                    IdStatusDokAsal = "CAPEX.WAITING_SUPERVISOR",
                    IdRole = 1,
                    IdStatusDok = "CAPEX.REVISED",
                    IdForm = 501,
                    Nama = "Minta Perbaikan / Revisi",
                    ClassButton = "btn-warning",
                    ClassIcon = "fa fa-reply",
                    ActionType = WorkflowActionType.Revise,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = false,
                    IsKirimAtasan = false,
                    IsKirimPembuat = true,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = 72,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 3,
                    IsActive = true
                },
                // Step 4: Direksi Keuangan -> Persetujuan Final (Approve)
                new()
                {
                    IdTipeDisposisi = 201,
                    IdRoleAsal = 5,
                    IdStatusDokAsal = "CAPEX.WAITING_FINANCE_DIR",
                    IdRole = 5,
                    IdStatusDok = "CAPEX.APPROVED",
                    IdForm = 501,
                    Nama = "Persetujuan Final (Approve)",
                    ClassButton = "btn-success",
                    ClassIcon = "fa fa-check-double",
                    ActionType = WorkflowActionType.Approve,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = true,
                    IsKirimAtasan = false,
                    IsKirimPembuat = false,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = null,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 4,
                    IsActive = true
                },
                // Step 5: Direksi Keuangan -> Tolak Pengajuan (Reject)
                new()
                {
                    IdTipeDisposisi = 201,
                    IdRoleAsal = 5,
                    IdStatusDokAsal = "CAPEX.WAITING_FINANCE_DIR",
                    IdRole = 1,
                    IdStatusDok = "CAPEX.REJECTED",
                    IdForm = 501,
                    Nama = "Tolak Pengajuan (Reject)",
                    ClassButton = "btn-danger",
                    ClassIcon = "fa fa-times",
                    ActionType = WorkflowActionType.Reject,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = true,
                    IsKirimAtasan = false,
                    IsKirimPembuat = true,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = true, // Wajib lampirkan alasan telaah
                    SlaHoursDefault = null,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 5,
                    IsActive = true
                },

                // ==========================================
                // MODUL 101: PROCUREMENT PR
                // ==========================================
                // Step 1: Draft PR -> Submit
                new()
                {
                    IdTipeDisposisi = 101,
                    IdRoleAsal = 1,
                    IdStatusDokAsal = "PR.DRAFT",
                    IdRole = 2,
                    IdStatusDok = "PR.WAITING_APPROVAL",
                    IdForm = 101,
                    Nama = "Kirim Pengajuan PR",
                    ClassButton = "btn-primary",
                    ClassIcon = "fa fa-paper-plane",
                    ActionType = WorkflowActionType.Submit,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = "internalsave",
                    UiFormSeting = "insertable",
                    IsAwal = true,
                    IsAkhir = false,
                    IsKirimAtasan = true,
                    IsKirimPembuat = false,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = 24,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 1,
                    IsActive = true
                },
                // Step 2: Section Head Approve PR
                new()
                {
                    IdTipeDisposisi = 101,
                    IdRoleAsal = 2,
                    IdStatusDokAsal = "PR.WAITING_APPROVAL",
                    IdRole = 2,
                    IdStatusDok = "PR.APPROVED",
                    IdForm = 101,
                    Nama = "Setujui PR (Approve)",
                    ClassButton = "btn-success",
                    ClassIcon = "fa fa-check",
                    ActionType = WorkflowActionType.Approve,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = true,
                    IsKirimAtasan = false,
                    IsKirimPembuat = false,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = null,
                    AmtLimitMin = 0,
                    AmtLimitMax = 50000000, // Batas wewenang s.d 50 Juta
                    NoUrutan = 2,
                    IsActive = true
                },
                // Step 3: Section Head Reject PR
                new()
                {
                    IdTipeDisposisi = 101,
                    IdRoleAsal = 2,
                    IdStatusDokAsal = "PR.WAITING_APPROVAL",
                    IdRole = 1,
                    IdStatusDok = "PR.REJECTED",
                    IdForm = 101,
                    Nama = "Tolak PR",
                    ClassButton = "btn-danger",
                    ClassIcon = "fa fa-times",
                    ActionType = WorkflowActionType.Reject,
                    ApprovalMode = ApprovalMode.Single,
                    UiFormAction = null,
                    UiFormSeting = "readonly",
                    IsAwal = false,
                    IsAkhir = true,
                    IsKirimAtasan = false,
                    IsKirimPembuat = true,
                    IsKirimSebelumnya = false,
                    IsRequireAttachment = false,
                    SlaHoursDefault = null,
                    AmtLimitMin = 0,
                    AmtLimitMax = 0,
                    NoUrutan = 3,
                    IsActive = true
                }
            };

            await context.WmUrutanDisposisi.AddRangeAsync(urutanList);
            await context.SaveChangesAsync();
        }

        // 3. Master Delegasi Wewenang Contoh (Plt Siti Rahmawati ke Rina Marlina)
        if (!await context.WmWorkflowDelegation.AnyAsync())
        {
            var delegation = new WmWorkflowDelegation
            {
                IdUserPemberi = 2, // Siti Rahmawati
                IdUserPenerima = 6, // Rina Marlina (Plt)
                IdTipeDisposisi = null, // Berlaku untuk semua tipe
                TanggalMulai = DateTime.UtcNow.AddDays(-1),
                TanggalSelesai = DateTime.UtcNow.AddDays(14),
                AlasanDelegasi = "Cuti Tahunan & Perjalanan Dinas Luar Kota",
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = 2
            };

            await context.WmWorkflowDelegation.AddAsync(delegation);
            await context.SaveChangesAsync();
        }
    }
}
