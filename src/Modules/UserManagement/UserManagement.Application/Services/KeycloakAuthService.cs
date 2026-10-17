using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;

namespace UserManagement.Application.Services;

/// <summary>
/// Implementasi layanan autentikasi Keycloak.
/// Menangani login (Direct Grant), registrasi, refresh token,
/// logout dengan blacklist, dan pengayaan JWT claims dengan data staf.
/// </summary>
public sealed class KeycloakAuthService : IKeycloakAuthService
{
    private readonly IKeycloakHttpClient _keycloakClient;
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly IUserManagementDbContext _dbContext;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly ILogger<KeycloakAuthService> _logger;

    public KeycloakAuthService(
        IKeycloakHttpClient keycloakClient,
        IKeycloakAdminService keycloakAdmin,
        IUserManagementDbContext dbContext,
        ITokenBlacklistService tokenBlacklist,
        ILogger<KeycloakAuthService> logger)
    {
        _keycloakClient = keycloakClient;
        _keycloakAdmin = keycloakAdmin;
        _dbContext = dbContext;
        _tokenBlacklist = tokenBlacklist;
        _logger = logger;
    }

    public async Task<AuthTokenResponse> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        // Step 1: Authenticate via Keycloak Direct Grant
        var tokenResponse = await _keycloakClient.RequestTokenAsync(request.Username, request.Password, ct);

        // Step 2: Extract sub (Keycloak User ID) dari access token
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenResponse.AccessToken);
        var keycloakUserId = jwt.Subject ?? throw new InvalidOperationException("Token tidak mengandung claim 'sub'.");

        // Step 3: Cari data staf yang terhubung
        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId && s.IsActive, ct);

        // Step 4: Extract roles
        var roles = ExtractRoles(jwt);

        // Step 5: Compose response
        var userInfo = new UserInfoDto(
            keycloakUserId,
            request.Username,
            jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
            roles,
            staff is not null
                ? new StaffInfoDto(
                    staff.IdStaf,
                    staff.Nip,
                    staff.NamaLengkap,
                    staff.KodeDepartemen,
                    staff.KodeJabatan,
                    staff.IdAtasanLangsung)
                : null);

        _logger.LogInformation(
            "Login berhasil: User={Username}, StaffId={StaffId}",
            request.Username, staff?.IdStaf);

        return new AuthTokenResponse(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn,
            tokenResponse.RefreshExpiresIn,
            "Bearer",
            userInfo);
    }

    public async Task<AuthTokenResponse> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        // Step 1: Validasi staf berdasarkan NIP & Email
        var staff = await _dbContext.MStaf
            .FirstOrDefaultAsync(s => s.Nip == request.Nip && s.Email == request.Email && s.IsActive, ct)
            ?? throw new InvalidOperationException(
                $"Data staf dengan NIP '{request.Nip}' dan email yang diberikan tidak ditemukan atau tidak aktif. " +
                "Registrasi mandiri hanya diperbolehkan untuk karyawan aktif yang terdaftar.");

        if (!string.IsNullOrEmpty(staff.KeycloakUserId))
            throw new InvalidOperationException(
                $"Staf NIP '{request.Nip}' sudah memiliki akun terdaftar. " +
                "Hubungi administrator untuk reset password.");

        // Step 2: Buat akun Keycloak
        var nameParts = request.NamaLengkap.Split(' ', 2);
        var keycloakUserId = await _keycloakAdmin.CreateUserAsync(
            new CreateKeycloakUserRequest(
                request.Username,
                request.Email,
                request.Password,
                nameParts[0],
                nameParts.Length > 1 ? nameParts[1] : string.Empty),
            ct);

        // Step 3: Assign default role
        await _keycloakAdmin.AssignRealmRoleAsync(keycloakUserId, "ROLE_USER", ct);

        // Step 4: Link ke staf
        staff.KeycloakUserId = keycloakUserId;
        staff.UpdatedDate = DateTime.UtcNow;
        staff.UpdatedBy = request.Username;
        await _dbContext.SaveChangesAsync(ct);

        // Step 5: Login otomatis setelah registrasi
        return await LoginAsync(new LoginRequest(request.Username, request.Password), ct);
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var tokenResponse = await _keycloakClient.RefreshTokenAsync(refreshToken, ct);

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(tokenResponse.AccessToken);
        var keycloakUserId = jwt.Subject ?? string.Empty;

        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId && s.IsActive, ct);

        var roles = ExtractRoles(jwt);

        var userInfo = new UserInfoDto(
            keycloakUserId,
            jwt.Claims.FirstOrDefault(c => c.Type == "preferred_username")?.Value ?? string.Empty,
            jwt.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? string.Empty,
            roles,
            staff is not null
                ? new StaffInfoDto(
                    staff.IdStaf, staff.Nip, staff.NamaLengkap,
                    staff.KodeDepartemen, staff.KodeJabatan, staff.IdAtasanLangsung)
                : null);

        return new AuthTokenResponse(
            tokenResponse.AccessToken,
            tokenResponse.RefreshToken,
            tokenResponse.ExpiresIn,
            tokenResponse.RefreshExpiresIn,
            "Bearer",
            userInfo);
    }

    public async Task ChangePasswordAsync(
        string keycloakUserId, string currentPassword, string newPassword, CancellationToken ct)
    {
        // Verify current password by attempting login
        var user = await _keycloakAdmin.GetUserByIdAsync(keycloakUserId, ct)
            ?? throw new InvalidOperationException("User Keycloak tidak ditemukan.");

        try
        {
            await _keycloakClient.RequestTokenAsync(user.Username, currentPassword, ct);
        }
        catch
        {
            throw new InvalidOperationException("Password saat ini tidak valid.");
        }

        // Update password via Admin API
        await _keycloakClient.UpdatePasswordAsync(keycloakUserId, newPassword, ct);

        // Logout all sessions to force re-authentication
        await _keycloakAdmin.LogoutAllSessionsAsync(keycloakUserId, ct);

        _logger.LogInformation("Password berhasil diubah untuk user Keycloak: {UserId}", keycloakUserId);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Email == email && s.IsActive, ct);

        if (staff is null || string.IsNullOrEmpty(staff.KeycloakUserId))
        {
            // Jangan bocorkan informasi apakah email terdaftar (security best practice)
            _logger.LogInformation(
                "Forgot password requested for email: {MaskedEmail}",
                PiiMaskingUtility.MaskEmail(email));
            return;
        }

        await _keycloakAdmin.SendPasswordResetEmailAsync(staff.KeycloakUserId, ct);
    }

    public async Task LogoutAsync(string accessToken, string refreshToken, CancellationToken ct)
    {
        // Step 1: Extract JTI dari access token untuk blacklist
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(accessToken);
        var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
        var exp = jwt.ValidTo;

        if (!string.IsNullOrEmpty(jti) && exp > DateTime.UtcNow)
        {
            var remainingLifetime = exp - DateTime.UtcNow;
            await _tokenBlacklist.BlacklistTokenAsync(jti, remainingLifetime, ct);
        }

        // Step 2: Revoke refresh token di Keycloak
        try
        {
            await _keycloakClient.RevokeTokenAsync(refreshToken, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gagal merevokasi refresh token di Keycloak.");
        }

        _logger.LogInformation("Logout berhasil: JTI={Jti}", jti);
    }

    public async Task<UserProfileResponse> GetCurrentUserProfileAsync(string keycloakUserId, CancellationToken ct)
    {
        var keycloakUser = await _keycloakAdmin.GetUserByIdAsync(keycloakUserId, ct)
            ?? throw new InvalidOperationException("User Keycloak tidak ditemukan.");

        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId && s.IsActive, ct);

        return new UserProfileResponse(
            keycloakUserId,
            keycloakUser.Username,
            keycloakUser.Email ?? string.Empty,
            keycloakUser.RealmRoles,
            staff is not null
                ? new StaffInfoDto(
                    staff.IdStaf, staff.Nip, staff.NamaLengkap,
                    staff.KodeDepartemen, staff.KodeJabatan, staff.IdAtasanLangsung)
                : null);
    }

    private static IReadOnlyList<string> ExtractRoles(JwtSecurityToken jwt)
    {
        var realmAccessClaim = jwt.Claims.FirstOrDefault(c => c.Type == "realm_access")?.Value;
        if (string.IsNullOrEmpty(realmAccessClaim))
            return Array.Empty<string>();

        try
        {
            using var doc = JsonDocument.Parse(realmAccessClaim);
            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
            {
                return rolesElement.EnumerateArray()
                    .Select(r => r.GetString()!)
                    .Where(r => !string.IsNullOrEmpty(r))
                    .ToList()
                    .AsReadOnly();
            }
        }
        catch
        {
            // Fallback: parse individual role claims
        }

        return jwt.Claims
            .Where(c => c.Type == "role" || c.Type == "roles")
            .Select(c => c.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();
    }
}

/// <summary>
/// Kontrak HTTP client ke Keycloak Token/Admin Endpoints.
/// Diimplementasikan pada Infrastructure layer (KeycloakHttpClient).
/// </summary>
public interface IKeycloakHttpClient
{
    Task<KeycloakTokenResponse> RequestTokenAsync(string username, string password, CancellationToken ct = default);
    Task<KeycloakTokenResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string token, CancellationToken ct = default);
    Task UpdatePasswordAsync(string keycloakUserId, string newPassword, CancellationToken ct = default);
}

public sealed record KeycloakTokenResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    int RefreshExpiresIn);
