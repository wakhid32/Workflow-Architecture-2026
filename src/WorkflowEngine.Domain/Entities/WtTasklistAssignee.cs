using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Mendukung penugasan multi-approver untuk satu step workflow.
/// Digunakan pada mode ANY_PARALLEL (siapa cepat) atau ALL_PARALLEL (konsensus).
/// </summary>
public sealed class WtTasklistAssignee
{
    public long IdAssignee { get; set; }
    public long IdTask { get; set; }
    public int IdUrutanDisp { get; set; }
    public long IdUser { get; set; }
    public AssigneeApprovalStatus StatusApproval { get; set; } = AssigneeApprovalStatus.Pending;
    public DateTime? TglRespon { get; set; }
    public string? Catatan { get; set; }
    public bool IsDelegated { get; set; }
    public long? IdDelegatedFrom { get; set; }

    // Navigation
    public WtTasklist Tasklist { get; set; } = null!;
}
