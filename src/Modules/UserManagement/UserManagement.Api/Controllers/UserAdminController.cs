using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;

namespace UserManagement.Api.Controllers;

/// <summary>
/// Controller administrasi user Keycloak (/api/v1/users).
/// Menangani operasi admin: enable/disable akun, role assignment,
/// dan force logout sesi. Akses SuperAdmin only.
/// </summary>
[ApiController]
[Route("api/v1/users")]
[Produces("application/json")]
[Authorize(Roles = "ROLE_SUPERADMIN")]
public sealed class UserAdminController : ControllerBase
{
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly ILogger<UserAdminController> _logger;

    public UserAdminController(
        IKeycloakAdminService keycloakAdmin,
        ILogger<UserAdminController> logger)
    {
        _keycloakAdmin = keycloakAdmin;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/v1/users/{keycloakUserId} — Mengambil informasi user dari Keycloak.
    /// </summary>
    [HttpGet("{keycloakUserId}")]
    [ProducesResponseType(typeof(BaseResponse<KeycloakUserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser([FromRoute] string keycloakUserId, CancellationToken ct)
    {
        var user = await _keycloakAdmin.GetUserByIdAsync(keycloakUserId, ct);
        if (user is null)
            return NotFound(BaseResponse<object>.Fail("User Keycloak tidak ditemukan."));

        return Ok(BaseResponse<KeycloakUserDto>.Ok(user));
    }

    /// <summary>
    /// POST /api/v1/users/{keycloakUserId}/disable — Menonaktifkan akun user Keycloak.
    /// </summary>
    [HttpPost("{keycloakUserId}/disable")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DisableUser([FromRoute] string keycloakUserId, CancellationToken ct)
    {
        await _keycloakAdmin.DisableUserAsync(keycloakUserId, ct);
        return Ok(BaseResponse<object>.Ok(null!, "Akun Keycloak berhasil dinonaktifkan."));
    }

    /// <summary>
    /// POST /api/v1/users/{keycloakUserId}/enable — Mengaktifkan kembali akun user Keycloak.
    /// </summary>
    [HttpPost("{keycloakUserId}/enable")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EnableUser([FromRoute] string keycloakUserId, CancellationToken ct)
    {
        await _keycloakAdmin.EnableUserAsync(keycloakUserId, ct);
        return Ok(BaseResponse<object>.Ok(null!, "Akun Keycloak berhasil diaktifkan kembali."));
    }

    /// <summary>
    /// POST /api/v1/users/{keycloakUserId}/assign-role — Menetapkan role realm ke user.
    /// </summary>
    [HttpPost("{keycloakUserId}/assign-role")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRole(
        [FromRoute] string keycloakUserId,
        [FromBody] AssignRoleRequest request,
        CancellationToken ct)
    {
        await _keycloakAdmin.AssignRealmRoleAsync(keycloakUserId, request.RoleName, ct);
        return Ok(BaseResponse<object>.Ok(null!, $"Role '{request.RoleName}' berhasil ditetapkan."));
    }

    /// <summary>
    /// POST /api/v1/users/{keycloakUserId}/force-logout — Menghapus seluruh sesi aktif user.
    /// </summary>
    [HttpPost("{keycloakUserId}/force-logout")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ForceLogout([FromRoute] string keycloakUserId, CancellationToken ct)
    {
        await _keycloakAdmin.LogoutAllSessionsAsync(keycloakUserId, ct);
        return Ok(BaseResponse<object>.Ok(null!, "Seluruh sesi aktif user berhasil dihapus."));
    }
}

// ─── Request DTOs ────────────────────────────────────────────────────────────

public sealed record AssignRoleRequest(string RoleName);
