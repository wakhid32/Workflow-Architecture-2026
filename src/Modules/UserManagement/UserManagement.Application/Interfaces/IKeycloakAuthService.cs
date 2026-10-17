namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak layanan autentikasi Keycloak.
/// Menangani alur login (Direct Grant), registrasi, refresh token,
/// perubahan password, dan revokasi sesi.
/// </summary>
public interface IKeycloakAuthService
{
    /// <summary>
    /// Autentikasi pengguna via Resource Owner Password Credentials (Direct Grant).
    /// Mengembalikan access token, refresh token, dan data pengguna yang diperkaya klaim staf.
    /// </summary>
    Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>
    /// Mendaftarkan pengguna baru di Keycloak dan menautkan ke data staf internal.
    /// Memvalidasi keberadaan NIP/Email di M_STAF sebelum membuat akun Keycloak.
    /// </summary>
    Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);

    /// <summary>
    /// Menukar refresh token yang masih berlaku untuk mendapatkan access token baru.
    /// </summary>
    Task<AuthTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Mengubah password pengguna yang sudah terautentikasi.
    /// Otomatis merevokasi seluruh sesi aktif dan me-blacklist token lama.
    /// </summary>
    Task ChangePasswordAsync(string keycloakUserId, string currentPassword, string newPassword, CancellationToken ct = default);

    /// <summary>
    /// Memicu alur reset password melalui email via Keycloak.
    /// </summary>
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);

    /// <summary>
    /// Melakukan logout: revokasi refresh token di Keycloak dan blacklist access token di Redis.
    /// </summary>
    Task LogoutAsync(string accessToken, string refreshToken, CancellationToken ct = default);

    /// <summary>
    /// Mengambil profil pengguna aktif beserta data staf dan roles dari Keycloak.
    /// </summary>
    Task<UserProfileResponse> GetCurrentUserProfileAsync(string keycloakUserId, CancellationToken ct = default);
}

// ─── Request / Response DTOs ─────────────────────────────────────────────────

public sealed record LoginRequest(string Username, string Password);

public sealed record RegisterRequest(
    string Nip,
    string Email,
    string Username,
    string Password,
    string NamaLengkap);

public sealed record AuthTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresInSeconds,
    int RefreshExpiresInSeconds,
    string TokenType,
    UserInfoDto User);

public sealed record UserInfoDto(
    string KeycloakUserId,
    string Username,
    string Email,
    IReadOnlyList<string> Roles,
    StaffInfoDto? StaffInfo);

public sealed record StaffInfoDto(
    long IdStaf,
    string Nip,
    string NamaLengkap,
    string KodeDepartemen,
    string KodeJabatan,
    long? IdAtasanLangsung);

public sealed record UserProfileResponse(
    string KeycloakUserId,
    string Username,
    string Email,
    IReadOnlyList<string> Roles,
    StaffInfoDto? StaffInfo);
