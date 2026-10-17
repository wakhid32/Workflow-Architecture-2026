namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Status pengiriman event pada outbox notifikasi.
/// </summary>
public enum OutboxStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
