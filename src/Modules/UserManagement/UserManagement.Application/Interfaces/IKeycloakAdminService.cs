namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak layanan administrasi Keycloak via Admin REST API.
/// Menangani sinkronisasi user, pengelolaan roles, dan pengendalian akun.
/// </summary>
public interface IKeycloakAdminService
{
    /// <summary>
    /// Membuat user baru di Keycloak realm dan mengembalikan User ID (UUID).
    /// </summary>
    Task<string> CreateUserAsync(CreateKeycloakUserRequest request, CancellationToken ct = default);

    /// <summary>
    /// Menetapkan role realm kepada user Keycloak.
    /// </summary>
    Task AssignRealmRoleAsync(string keycloakUserId, string roleName, CancellationToken ct = default);

    /// <summary>
    /// Menonaktifkan (disable) akun user Keycloak.
    /// Semua sesi aktif otomatis diputuskan.
    /// </summary>
    Task DisableUserAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Mengaktifkan kembali akun user Keycloak yang sebelumnya dinonaktifkan.
    /// </summary>
    Task EnableUserAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Menghapus seluruh sesi aktif user dari Keycloak.
    /// </summary>
    Task LogoutAllSessionsAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Mengambil informasi user dari Keycloak berdasarkan User ID.
    /// </summary>
    Task<KeycloakUserDto?> GetUserByIdAsync(string keycloakUserId, CancellationToken ct = default);

    /// <summary>
    /// Mengirim email reset password ke user Keycloak.
    /// </summary>
    Task SendPasswordResetEmailAsync(string keycloakUserId, CancellationToken ct = default);
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record CreateKeycloakUserRequest(
    string Username,
    string Email,
    string Password,
    string FirstName,
    string LastName,
    bool Enabled = true);

public sealed record KeycloakUserDto(
    string Id,
    string Username,
    string Email,
    string? FirstName,
    string? LastName,
    bool Enabled,
    IReadOnlyList<string> RealmRoles);
