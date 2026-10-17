using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Audit trail riwayat disposisi. Setiap transisi state tercatat beserta
/// metadata operasional: durasi, IP, user agent, dan jumlah lampiran.
/// </summary>
public sealed class WtTasklistHist
{
    public long IdTaskHist { get; set; }
    public long IdTask { get; set; }
    public int IdUrutanDisp { get; set; }
    public long IdUserAsal { get; set; }
    public long IdUserTujuan { get; set; }
    public long IdUserEksekutor { get; set; }
    public string IdStatusDok { get; set; } = string.Empty;
    public WorkflowActionType ActionTaken { get; set; }
    public DateTime TglTaskMulai { get; set; }
    public DateTime TglTaskAkhir { get; set; } = DateTime.UtcNow;
    public int DurationMinutes { get; set; }
    public string? TaskInstanceJson { get; set; }
    public int TotalAttachments { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }

    // Navigation
    public WtTasklist Tasklist { get; set; } = null!;
    public ICollection<WtTasklistAttachment> Attachments { get; set; } = new List<WtTasklistAttachment>();
}
