using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak layanan manajemen data master karyawan (M_STAF).
/// Menangani CRUD staf, linking/unlinking akun Keycloak,
/// dan one-step provisioning.
/// </summary>
public interface IStaffManagementService
{
    Task<StaffDetailDto> GetStaffByIdAsync(long staffId, CancellationToken ct = default);
    Task<PagedResult<StaffListItemDto>> GetAllStaffAsync(StaffQueryRequest query, CancellationToken ct = default);
    Task<StaffDetailDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct = default);
    Task<StaffDetailDto> UpdateStaffAsync(long staffId, UpdateStaffRequest request, CancellationToken ct = default);
    Task DeleteStaffAsync(long staffId, CancellationToken ct = default);
    Task LinkKeycloakUserAsync(long staffId, string keycloakUserId, CancellationToken ct = default);
    Task UnlinkKeycloakUserAsync(long staffId, CancellationToken ct = default);
    Task<StaffDetailDto> ProvisionStaffWithAccountAsync(ProvisionStaffRequest request, CancellationToken ct = default);
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record StaffQueryRequest(
    string? SearchTerm = null,
    string? KodeDepartemen = null,
    string? KodeJabatan = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 20);

public sealed record CreateStaffRequest(
    string Nip,
    string NamaLengkap,
    string Email,
    string? NomorHp,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung);

public sealed record UpdateStaffRequest(
    string NamaLengkap,
    string Email,
    string? NomorHp,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung);

public sealed record ProvisionStaffRequest(
    string Nip,
    string NamaLengkap,
    string Email,
    string? NomorHp,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung,
    string Username,
    string Password,
    string RoleName = "ROLE_USER");

public sealed record StaffDetailDto(
    long IdStaf,
    string Nip,
    string NamaLengkap,
    string Email,
    string? NomorHp,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung,
    string? KeycloakUserId,
    bool IsActive,
    DateTime CreatedDate,
    DateTime UpdatedDate);

public sealed record StaffListItemDto(
    long IdStaf,
    string Nip,
    string NamaLengkap,
    string Email,
    string KodeDepartemen,
    string KodeJabatan,
    bool IsActive,
    bool HasKeycloakAccount);

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
