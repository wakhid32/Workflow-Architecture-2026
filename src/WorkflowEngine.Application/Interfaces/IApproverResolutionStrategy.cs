using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Kontrak strategi resolusi kandidat approver/atasan/matrix wewenang
/// termasuk pengecekan delegasi wewenang (Plt/Pjs).
/// </summary>
public interface IApproverResolutionStrategy
{
    /// <summary>
    /// Mengambil kandidat penerima/atasan awal saat inisiasi dokumen.
    /// </summary>
    Task<List<ApproverCandidateDto>> ResolveCandidatesForStartAsync(long userId, int idUrutanDisp, CancellationToken ct = default);

    /// <summary>
    /// Mengambil kandidat penerima untuk langkah berikutnya berdasarkan role, departemen, dan delegasi.
    /// </summary>
    Task<List<ApproverCandidateDto>> ResolveCandidatesForStepAsync(int idUrutanDisp, long currentUserId, CancellationToken ct = default);

    /// <summary>
    /// Memeriksa apakah seorang user memiliki wewenang persetujuan (baik sebagai PIC langsung maupun delegasi aktif).
    /// </summary>
    Task<bool> HasApprovalAuthorityAsync(long currentUserId, long picUserId, int idTipeDisposisi, CancellationToken ct = default);
}
