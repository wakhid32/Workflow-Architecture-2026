namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak layanan pembaca data staf — dirancang sebagai service boundary yang dapat dikonsumsi
/// oleh modul eksternal (WorkflowEngine). Implementasi menggunakan Cache-Aside Pattern (L2 Redis).
/// </summary>
public interface IStaffDirectoryService
{
    /// <summary>
    /// Mengambil ringkasan staf berdasarkan ID. Didukung distributed caching.
    /// </summary>
    Task<StaffSummaryDto?> GetStaffByIdAsync(long staffId, CancellationToken ct = default);

    /// <summary>
    /// Mengambil data atasan langsung dari staf yang dimaksud.
    /// </summary>
    Task<StaffSummaryDto?> GetImmediateSupervisorAsync(long staffId, CancellationToken ct = default);

    /// <summary>
    /// Mengambil daftar staf berdasarkan departemen dan jabatan.
    /// Digunakan oleh workflow engine untuk resolusi approver.
    /// </summary>
    Task<IReadOnlyList<StaffSummaryDto>> GetStaffByDepartmentAndPositionAsync(
        string departmentCode, string positionCode, CancellationToken ct = default);
}

public sealed record StaffSummaryDto(
    long IdStaf,
    string Nip,
    string NamaLengkap,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung,
    bool IsActive);
