namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Master delegasi wewenang approval. Mengatur pelimpahan
/// hak persetujuan dari pejabat asli ke pengganti sementara (Plt/Pjs)
/// dengan rentang waktu dan tipe workflow yang dapat ditentukan.
/// </summary>
public sealed class WmWorkflowDelegation
{
    public long IdDelegation { get; set; }
    public long IdUserPemberi { get; set; }
    public long IdUserPenerima { get; set; }

    /// <summary>
    /// Null = berlaku untuk semua tipe workflow.
    /// </summary>
    public int? IdTipeDisposisi { get; set; }

    public DateTime TanggalMulai { get; set; }
    public DateTime TanggalSelesai { get; set; }
    public string AlasanDelegasi { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }

    // Navigation
    public WmTipeDisposisi? TipeDisposisi { get; set; }
}
