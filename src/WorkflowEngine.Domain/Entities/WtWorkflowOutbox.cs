using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Transactional Outbox Pattern: mencatat event notifikasi yang harus dikirim
/// secara asinkron oleh background processor. Menjamin eventual consistency
/// tanpa mengganggu transaksi database utama.
/// </summary>
public sealed class WtWorkflowOutbox
{
    public long IdOutbox { get; set; }
    public long IdTask { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string RecipientPayloadJson { get; set; } = "{}";
    public string MessagePayloadJson { get; set; } = "{}";
    public OutboxStatus Status { get; set; } = OutboxStatus.Pending;
    public int RetryCount { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedDate { get; set; }
    public string? LastError { get; set; }

    // Navigation
    public WtTasklist Tasklist { get; set; } = null!;
}
