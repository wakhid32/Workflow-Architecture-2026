using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Master konfigurasi state-machine: setiap baris merepresentasikan satu tombol aksi
/// pada suatu tahapan workflow tertentu. Berisi aturan transisi status, role target,
/// batas wewenang nominal, SLA default, dan pengaturan UI.
/// </summary>
public sealed class WmUrutanDisposisi
{
    public int IdUrutanDisp { get; set; }
    public int IdTipeDisposisi { get; set; }
    public int IdRoleAsal { get; set; }
    public string IdStatusDokAsal { get; set; } = string.Empty;
    public int IdRole { get; set; }
    public string IdStatusDok { get; set; } = string.Empty;
    public int IdForm { get; set; }
    public string? KodeDepartemen { get; set; }
    public string? KodeJabatan { get; set; }

    // Tampilan Tombol
    public string Nama { get; set; } = string.Empty;
    public string ClassButton { get; set; } = "btn-primary";
    public string ClassIcon { get; set; } = "fa fa-check";

    // Kategorisasi & Mode
    public WorkflowActionType ActionType { get; set; } = WorkflowActionType.Approve;
    public ApprovalMode ApprovalMode { get; set; } = ApprovalMode.Single;

    // Hook ke frontend form
    public string? UiFormAction { get; set; }
    public string UiFormSeting { get; set; } = "readonly";

    // Flags kontrol alur
    public bool IsAwal { get; set; }
    public bool IsAkhir { get; set; }
    public bool IsKirimAtasan { get; set; }
    public bool IsKirimPembuat { get; set; }
    public bool IsKirimSebelumnya { get; set; }
    public bool IsRequireAttachment { get; set; }

    // SLA & Authority Limit
    public int? SlaHoursDefault { get; set; }
    public decimal AmtLimitMin { get; set; }
    public decimal AmtLimitMax { get; set; }

    // Kondisi dinamis JSON
    public string? ConditionExpressionJson { get; set; }

    public int NoUrutan { get; set; } = 1;
    public bool IsActive { get; set; } = true;

    // Navigation
    public WmTipeDisposisi TipeDisposisi { get; set; } = null!;
}
