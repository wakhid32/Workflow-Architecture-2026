using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.Workflows.DTOs;

// ==========================================
// TIPE DISPOSISI DTOs
// ==========================================

public record TipeDisposisiDto(
    int IdTipeDisposisi,
    string NamaDisposisi,
    string? Deskripsi,
    string KodeModul,
    bool IsActive,
    bool IsReopenClosed,
    DateTime CreatedDate,
    string? CreatedBy,
    int TotalStepsCount
);

public record CreateTipeDisposisiRequest(
    int? IdTipeDisposisi,
    string NamaDisposisi,
    string? Deskripsi,
    string KodeModul,
    bool IsActive = true,
    bool IsReopenClosed = false
);

public record UpdateTipeDisposisiRequest(
    string NamaDisposisi,
    string? Deskripsi,
    string KodeModul,
    bool IsActive,
    bool IsReopenClosed
);

// ==========================================
// URUTAN DISPOSISI (STATE MACHINE) DTOs
// ==========================================

public record UrutanDisposisiDetailDto(
    int IdUrutanDisp,
    int IdTipeDisposisi,
    string NamaTipeDisposisi,
    string KodeModul,
    int IdRoleAsal,
    string IdStatusDokAsal,
    int IdRole,
    string IdStatusDok,
    int IdForm,
    string? KodeDepartemen,
    string? KodeJabatan,
    string Nama,
    string ClassButton,
    string ClassIcon,
    WorkflowActionType ActionType,
    ApprovalMode ApprovalMode,
    string? UiFormAction,
    string UiFormSeting,
    bool IsAwal,
    bool IsAkhir,
    bool IsKirimAtasan,
    bool IsKirimPembuat,
    bool IsKirimSebelumnya,
    bool IsRequireAttachment,
    int? SlaHoursDefault,
    decimal AmtLimitMin,
    decimal AmtLimitMax,
    string? ConditionExpressionJson,
    int NoUrutan,
    bool IsActive
);

public record CreateUrutanDisposisiRequest(
    int IdTipeDisposisi,
    int IdRoleAsal,
    string IdStatusDokAsal,
    int IdRole,
    string IdStatusDok,
    int IdForm,
    string Nama,
    string? KodeDepartemen = null,
    string? KodeJabatan = null,
    string ClassButton = "btn-primary",
    string ClassIcon = "fa fa-check",
    WorkflowActionType ActionType = WorkflowActionType.Approve,
    ApprovalMode ApprovalMode = ApprovalMode.Single,
    string? UiFormAction = null,
    string UiFormSeting = "readonly",
    bool IsAwal = false,
    bool IsAkhir = false,
    bool IsKirimAtasan = false,
    bool IsKirimPembuat = false,
    bool IsKirimSebelumnya = false,
    bool IsRequireAttachment = false,
    int? SlaHoursDefault = null,
    decimal AmtLimitMin = 0,
    decimal AmtLimitMax = 0,
    string? ConditionExpressionJson = null,
    int NoUrutan = 1,
    bool IsActive = true
);

public record UpdateUrutanDisposisiRequest(
    int IdRoleAsal,
    string IdStatusDokAsal,
    int IdRole,
    string IdStatusDok,
    int IdForm,
    string Nama,
    string? KodeDepartemen,
    string? KodeJabatan,
    string ClassButton,
    string ClassIcon,
    WorkflowActionType ActionType,
    ApprovalMode ApprovalMode,
    string? UiFormAction,
    string UiFormSeting,
    bool IsAwal,
    bool IsAkhir,
    bool IsKirimAtasan,
    bool IsKirimPembuat,
    bool IsKirimSebelumnya,
    bool IsRequireAttachment,
    int? SlaHoursDefault,
    decimal AmtLimitMin,
    decimal AmtLimitMax,
    string? ConditionExpressionJson,
    int NoUrutan,
    bool IsActive
);

// ==========================================
// WORKFLOW DELEGATION (PLT) DTOs
// ==========================================

public record UpdateDelegationRequest(
    long IdUserPenerima,
    int? IdTipeDisposisi,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    string AlasanDelegasi,
    bool IsActive
);
