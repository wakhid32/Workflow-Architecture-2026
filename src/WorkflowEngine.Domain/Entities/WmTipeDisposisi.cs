namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Master tipe/jenis workflow disposisi. Setiap modul bisnis
/// (PR, PO, Reimbursement, Capex, dsb.) didaftarkan di sini.
/// </summary>
public sealed class WmTipeDisposisi
{
    public int IdTipeDisposisi { get; set; }
    public string NamaDisposisi { get; set; } = string.Empty;
    public string? Deskripsi { get; set; }
    public string KodeModul { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public bool IsReopenClosed { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }

    // Navigation
    public ICollection<WmUrutanDisposisi> UrutanDisposisiList { get; set; } = new List<WmUrutanDisposisi>();
    public ICollection<WtTasklist> TasklistList { get; set; } = new List<WtTasklist>();
}
