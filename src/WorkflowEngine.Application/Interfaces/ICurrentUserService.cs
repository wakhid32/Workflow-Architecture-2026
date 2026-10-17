namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Abstraksi untuk mendapatkan konteks pengguna yang sedang aktif/login.
/// Sesuai spesifikasi SECURITY.md Section 1.2 & Section 7.2.
/// Mendukung zero-DB-hop runtime execution via enriched claims.
/// </summary>
public interface ICurrentUserService
{
    long UserId { get; }
    string UserName { get; }
    string? UserNpk { get; }
    string? UserRole { get; }
    string? ClientIpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }

    /// <summary>
    /// ID Staf kanonikal dari tabel M_STAF (klaim: staff_id).
    /// </summary>
    long? StaffId => UserId;

    /// <summary>
    /// Nomor Induk Pegawai (klaim: staff_nip).
    /// </summary>
    string? StaffNip => UserNpk;

    /// <summary>
    /// Kode departemen staf aktif (klaim: department_code).
    /// </summary>
    string? DepartmentCode => null;

    /// <summary>
    /// Kode jabatan staf aktif (klaim: position_code).
    /// </summary>
    string? PositionCode => null;

    /// <summary>
    /// Keycloak User ID GUID (klaim: sub).
    /// </summary>
    string? KeycloakUserId => null;

    /// <summary>
    /// Alamat email terdaftar (klaim: email).
    /// </summary>
    string? Email => null;

    /// <summary>
    /// Daftar role yang dimiliki pengguna dari token JWT (klaim: realm_access.roles / role).
    /// </summary>
    IReadOnlyList<string> Roles => string.IsNullOrEmpty(UserRole) ? [] : [UserRole];
}

