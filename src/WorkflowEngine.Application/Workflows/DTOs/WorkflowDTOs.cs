namespace WorkflowEngine.Application.Workflows.DTOs;

// ==========================================
// START WORKFLOW
// ==========================================

public record StartWorkflowRequest(
    int TipeDisposisi,
    int? IdForm = null,
    string? IdDokumen = null,
    string? NoDokumen = null
);

public record WorkflowButtonDto(
    int IdUrutanDisp,
    string Nama,
    string ClassButton,
    string ClassIcon,
    string ActionType, // SUBMIT, APPROVE, REJECT, REVISE, CANCEL, DELEGATE, ESCALATE
    string? UiFormAction,
    string UiFormSeting,
    bool IsAkhir,
    bool IsKirimAtasan,
    bool IsKirimPembuat,
    bool IsKirimSebelumnya,
    bool IsRequireAttachment,
    int? SlaHoursDefault,
    decimal AmtLimitMin,
    decimal AmtLimitMax,
    string ApprovalMode = "SINGLE"
);

public record ApproverCandidateDto(
    long IdUser,
    string NamaLengkap,
    string Npk,
    string? Jabatan,
    string? Departemen,
    bool IsDelegated = false,
    long? DelegatedFromUserId = null,
    string? DelegatedFromName = null
);

public record StartWorkflowResponse(
    int TipeDisposisi,
    string NamaDisposisi,
    string StatusDokumenAwal,
    string UiFormSeting,
    List<WorkflowButtonDto> ActionButtons,
    List<ApproverCandidateDto> ApproverCandidates
);

// ==========================================
// GET TASK DETAIL & AUDIT TRAIL
// ==========================================

public record GetWorkflowTaskRequest(
    long? IdTask = null,
    string? HashId = null,
    string? IdDokumen = null,
    int? IdTipeDisposisi = null,
    int? IdForm = null
);

public record AttachmentSummaryDto(
    long IdAttachment,
    long IdTask,
    long? IdTaskHist,
    string FileName,
    string FileExtension,
    string ContentType,
    long FileSizeBytes,
    string? Caption,
    DateTime UploadedDate,
    long UploadedBy,
    string DownloadUrl
);

public record WorkflowAssigneeDto(
    long IdAssignee,
    long IdTask,
    int IdUrutanDisp,
    long IdUser,
    string NamaUser,
    string StatusApproval, // PENDING, APPROVED, REJECTED
    DateTime? TglRespon,
    string? Catatan,
    bool IsDelegated,
    long? IdDelegatedFrom
);

public record WorkflowAuditHistoryDto(
    long IdTaskHist,
    string ActionTaken,
    long IdUserAsal,
    string NamaPengirim,
    long IdUserTujuan,
    string NamaPenerima,
    long IdUserEksekutor,
    string NamaEksekutor,
    string StatusDokumen,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    int DurasiMenit,
    string? CatatanDisposisi,
    int TotalAttachments,
    string? ClientIp,
    string? UserAgent,
    List<AttachmentSummaryDto> Attachments
);

public record WorkflowTaskDetailResponse(
    long IdTask,
    string HashId,
    string IdDokumen,
    string NoDokumen,
    int IdForm,
    int IdTipeDisposisi,
    string NamaTipeDisposisi,
    string KodeModul,
    string StatusDokumen,
    string ApprovalStatus,
    string Priority,
    long IdUserAsal,
    string NamaUserAsal,
    long IdUserTujuan,
    string NamaUserTujuan,
    long? PrevIdUser,
    bool IsActivePic,
    string UiFormSeting,
    DateTime TanggalMulai,
    DateTime? TanggalSelesai,
    DateTime? DueDate,
    int? SlaHours,
    bool IsOverdue,
    string? CatatanTerakhir,
    byte[] RowVersion, // Concurrency Token
    List<WorkflowButtonDto> PermittedButtons,
    List<WorkflowAuditHistoryDto> AuditHistories,
    List<WorkflowAssigneeDto> Assignees,
    List<AttachmentSummaryDto> ActiveAttachments
);

// ==========================================
// EXECUTE NEXT DISPOSISI
// ==========================================

public record NextDisposisiRequest(
    long? IdTask,
    string? HashId,
    int IdUrutanDisposisi,
    string IdDokumen,
    string NoDokumen,
    int IdForm,
    long? SelectedApproverId = null,
    List<long>? MultiApproverIds = null,
    string? CatatanDisposisi = null,
    decimal DocumentAmount = 0,
    byte[]? RowVersion = null, // Token konkurensi dari frontend
    List<long>? UploadedAttachmentIds = null
);

public record NextDisposisiResult(
    bool Success,
    long IdTask,
    string HashId,
    string StatusDokumenBaru,
    string ApprovalStatus,
    bool IsCompleted,
    string Message,
    long TargetUserId,
    DateTime Timestamp
);

// ==========================================
// ATTACHMENT DTOs
// ==========================================

public record UploadAttachmentRequest(
    long IdTask,
    long? IdTaskHist = null,
    string? Caption = null
);

public record AttachmentResponseDto(
    long IdAttachment,
    long IdTask,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string DownloadUrl,
    DateTime UploadedDate
);

// ==========================================
// DELEGATION DTOs
// ==========================================

public record CreateDelegationRequest(
    long IdUserPenerima,
    int? IdTipeDisposisi,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    string AlasanDelegasi
);

public record DelegationResponseDto(
    long IdDelegation,
    long IdUserPemberi,
    string NamaPemberi,
    long IdUserPenerima,
    string NamaPenerima,
    int? IdTipeDisposisi,
    string? NamaTipeDisposisi,
    DateTime TanggalMulai,
    DateTime TanggalSelesai,
    string AlasanDelegasi,
    bool IsActive,
    DateTime CreatedDate
);
