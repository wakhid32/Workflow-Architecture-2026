namespace WorkflowEngine.Domain.Entities;

/// <summary>
/// Manajemen lampiran file pendukung disposisi.
/// Dapat dikaitkan dengan task aktif maupun riwayat arsip step sebelumnya.
/// </summary>
public sealed class WtTasklistAttachment
{
    public long IdAttachment { get; set; }
    public long IdTask { get; set; }
    public long? IdTaskHist { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileExtension { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string? Caption { get; set; }
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public long UploadedBy { get; set; }

    // Navigation
    public WtTasklist Tasklist { get; set; } = null!;
    public WtTasklistHist? TasklistHist { get; set; }
}
