using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Instansi aktif tasklist workflow. Merepresentasikan satu dokumen bisnis
/// yang sedang dalam proses persetujuan. Dilengkapi token konkurensi optimistik,
/// pelacakan SLA, dan status prioritas.
/// </summary>
public sealed class WtTasklist
{
    public long IdTask { get; set; }
    public string HashId { get; set; } = Guid.NewGuid().ToString("N");
    public int IdTipeDisposisi { get; set; }
    public int IdUrutanDisp { get; set; }
    public string IdDokumen { get; set; } = string.Empty;
    public string NoDokumen { get; set; } = string.Empty;
    public int IdForm { get; set; }
    public long IdUserAsal { get; set; }
    public long IdUserTujuan { get; set; }
    public long? PrevIdUser { get; set; }
    public string IdStatusDok { get; set; } = string.Empty;
    public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.InProgress;
    public TaskPriority Priority { get; set; } = TaskPriority.Normal;

    // Timestamps
    public DateTime TglDispMulai { get; set; } = DateTime.UtcNow;
    public DateTime? TglDispAkhir { get; set; }

    // SLA Tracking
    public DateTime? DueDate { get; set; }
    public int? SlaHours { get; set; }
    public bool IsOverdue { get; set; }

    // Content
    public string? TaskInstanceJson { get; set; }

    /// <summary>
    /// Token Optimistic Concurrency — mencegah race condition dan lost-update.
    /// Didukung di PostgreSQL, SQL Server, dan In-Memory via concurrency token.
    /// </summary>
    public byte[] RowVersion { get; set; } = Guid.NewGuid().ToByteArray();

    // Audit columns
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public long CreatedBy { get; set; }
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public long UpdatedBy { get; set; }

    // Navigation Properties
    public WmTipeDisposisi TipeDisposisi { get; set; } = null!;
    public WmUrutanDisposisi UrutanDisposisi { get; set; } = null!;
    public ICollection<WtTasklistHist> Histories { get; set; } = new List<WtTasklistHist>();
    public ICollection<WtTasklistAssignee> Assignees { get; set; } = new List<WtTasklistAssignee>();
    public ICollection<WtTasklistAttachment> Attachments { get; set; } = new List<WtTasklistAttachment>();
    public ICollection<WtWorkflowOutbox> OutboxMessages { get; set; } = new List<WtWorkflowOutbox>();
}
