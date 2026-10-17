namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Kategori aksi terstandarisasi pada setiap langkah disposisi workflow.
/// </summary>
public enum WorkflowActionType
{
    Submit = 0,
    Approve = 1,
    Reject = 2,
    Revise = 3,
    Cancel = 4,
    Delegate = 5,
    Escalate = 6
}
