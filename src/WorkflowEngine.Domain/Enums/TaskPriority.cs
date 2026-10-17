namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Prioritas dokumen dalam antrian approval.
/// </summary>
public enum TaskPriority : byte
{
    Low = 0,
    Normal = 1,
    High = 2,
    Urgent = 3
}
