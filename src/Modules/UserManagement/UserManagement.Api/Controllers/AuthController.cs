using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using UserManagement.Api.Middlewares;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;

namespace UserManagement.Api.Controllers;

/// <summary>
/// Controller autentikasi (/api/v1/auth).
/// Menangani login, registrasi mandiri, refresh token, logout, profile,
/// dan manajemen password. Semua endpoint dilindungi rate limiting.
/// Sesuai spesifikasi SECURITY.md Section 6.1.
/// </summary>
[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IKeycloakAuthService _authService;
    private readonly IUserManagementDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IKeycloakAuthService authService,
        IUserManagementDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// POST /api/v1/auth/register — Pendaftaran mandiri user staf baru.
    /// Rate limit: 3 req/jam/IP. Memvalidasi NIP/Email staf sebelum membuat akun Keycloak.
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRegisterPolicy")]
    [ProducesResponseType(typeof(BaseResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _authService.RegisterAsync(request, ct);

        await WriteAuditLogAsync("REGISTER", result.User.KeycloakUserId, "USER",
            result.User.KeycloakUserId, "SUCCESS");

        return Ok(BaseResponse<AuthTokenResponse>.Ok(result, "Registrasi berhasil."));
    }

    /// <summary>
    /// POST /api/v1/auth/login — Login pengguna (Direct Grant).
    /// Rate limit: 5 req/menit/IP. Menghasilkan JWT enriched dengan klaim staf.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthLoginPolicy")]
    [ProducesResponseType(typeof(BaseResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _authService.LoginAsync(request, ct);

            await WriteAuditLogAsync("LOGIN", result.User.KeycloakUserId, "SESSION",
                result.User.KeycloakUserId, "SUCCESS");

            return Ok(BaseResponse<AuthTokenResponse>.Ok(result, "Autentikasi berhasil."));
        }
        catch (InvalidOperationException ex)
        {
            await WriteAuditLogAsync("LOGIN", request.Username, "SESSION",
                request.Username, "FAILED");

            return BadRequest(BaseResponse<object>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// POST /api/v1/auth/refresh-token — Menukar Refresh Token untuk Access Token baru.
    /// Rate limit: 10 req/menit.
    /// </summary>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthRefreshPolicy")]
    [ProducesResponseType(typeof(BaseResponse<AuthTokenResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.RefreshTokenAsync(request.RefreshToken, ct);
        return Ok(BaseResponse<AuthTokenResponse>.Ok(result, "Token berhasil diperbarui."));
    }

    /// <summary>
    /// POST /api/v1/auth/forgot-password — Memicu email reset password via Keycloak.
    /// Rate limit: 3 req/15 menit.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting("AuthForgotPasswordPolicy")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _authService.ForgotPasswordAsync(request.Email, ct);

        // Selalu kembalikan respons sukses (jangan bocorkan apakah email terdaftar)
        return Ok(BaseResponse<object>.Ok(null!,
            "Jika email terdaftar dalam sistem, instruksi reset password telah dikirim."));
    }

    /// <summary>
    /// POST /api/v1/auth/change-password — Mengubah password akun aktif.
    /// Rate limit: 5 req/15 menit. Otomatis merevokasi sesi lama.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [EnableRateLimiting("AuthChangePasswordPolicy")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var keycloakUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? throw new UnauthorizedAccessException("User tidak terautentikasi.");

        await _authService.ChangePasswordAsync(keycloakUserId, request.CurrentPassword, request.NewPassword, ct);

        await WriteAuditLogAsync("CHANGE_PASSWORD", keycloakUserId, "USER", keycloakUserId, "SUCCESS");

        return Ok(BaseResponse<object>.Ok(null!,
            "Password berhasil diubah. Seluruh sesi aktif telah diputuskan. Silakan login kembali."));
    }

    /// <summary>
    /// GET /api/v1/auth/me — Profil user aktif beserta data staf dan roles.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<UserProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var keycloakUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub")
                          ?? throw new UnauthorizedAccessException("User tidak terautentikasi.");

        var profile = await _authService.GetCurrentUserProfileAsync(keycloakUserId, ct);
        return Ok(BaseResponse<UserProfileResponse>.Ok(profile));
    }

    /// <summary>
    /// POST /api/v1/auth/logout — Logout dengan revokasi sesi dan blacklist token.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request, CancellationToken ct)
    {
        var accessToken = HttpContext.Request.Headers.Authorization
            .FirstOrDefault()?.Replace("Bearer ", string.Empty) ?? string.Empty;

        await _authService.LogoutAsync(accessToken, request.RefreshToken, ct);

        var keycloakUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub") ?? "unknown";

        await WriteAuditLogAsync("LOGOUT", keycloakUserId, "SESSION", keycloakUserId, "SUCCESS");

        return Ok(BaseResponse<object>.Ok(null!, "Logout berhasil. Token telah direvokasi."));
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private async Task WriteAuditLogAsync(
        string actionName, string actorUserId, string resourceName,
        string? resourceId, string status)
    {
        try
        {
            var correlationId = HttpContext.GetCorrelationId();
            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var userAgent = HttpContext.Request.Headers.UserAgent.FirstOrDefault();

            _dbContext.SecAuditLog.Add(new Domain.Entities.SecAuditLog
            {
                CorrelationId = correlationId,
                ActionName = actionName,
                ActorUserId = actorUserId,
                ResourceName = resourceName,
                ResourceId = resourceId,
                Status = status,
                ClientIp = clientIp,
                UserAgent = userAgent
            });

            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gagal menulis audit log untuk aksi '{ActionName}'.", actionName);
        }
    }
}

// ─── Request DTOs (API Layer Only) ───────────────────────────────────────────

public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record ForgotPasswordRequest(string Email);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record LogoutRequest(string RefreshToken);
