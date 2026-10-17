namespace WorkflowEngine.Domain.Enums;

/// <summary>
/// Mode penugasan approver: tunggal, paralel siapa cepat, atau paralel semua harus setuju.
/// </summary>
public enum ApprovalMode
{
    Single = 0,
    AnyParallel = 1,
    AllParallel = 2
}
