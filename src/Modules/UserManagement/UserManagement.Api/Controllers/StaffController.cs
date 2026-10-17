using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;

namespace UserManagement.Api.Controllers;

/// <summary>
/// Controller manajemen data karyawan (/api/v1/staff).
/// Menangani CRUD staf, linking/unlinking Keycloak, dan one-step provisioning.
/// Akses dibatasi untuk SuperAdmin dan HRD.
/// Sesuai spesifikasi SECURITY.md Section 6.2.
/// </summary>
[ApiController]
[Route("api/v1/staff")]
[Produces("application/json")]
[Authorize(Roles = "ROLE_SUPERADMIN,ROLE_HRD")]
public sealed class StaffController : ControllerBase
{
    private readonly IStaffManagementService _staffService;

    public StaffController(IStaffManagementService staffService)
    {
        _staffService = staffService;
    }

    /// <summary>
    /// GET /api/v1/staff — Daftar seluruh karyawan (dengan pagination dan filter).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(BaseResponse<PagedResult<StaffListItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? searchTerm,
        [FromQuery] string? kodeDepartemen,
        [FromQuery] string? kodeJabatan,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new StaffQueryRequest(searchTerm, kodeDepartemen, kodeJabatan, isActive, page, pageSize);
        var result = await _staffService.GetAllStaffAsync(query, ct);
        return Ok(BaseResponse<PagedResult<StaffListItemDto>>.Ok(result));
    }

    /// <summary>
    /// GET /api/v1/staff/{id} — Detail karyawan beserta profil akun Keycloak terkait.
    /// </summary>
    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(BaseResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] long id, CancellationToken ct)
    {
        var result = await _staffService.GetStaffByIdAsync(id, ct);
        return Ok(BaseResponse<StaffDetailDto>.Ok(result));
    }

    /// <summary>
    /// POST /api/v1/staff — Menambahkan data master staf baru ke security_identity_db.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BaseResponse<StaffDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateStaffRequest request, CancellationToken ct)
    {
        var result = await _staffService.CreateStaffAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.IdStaf },
            BaseResponse<StaffDetailDto>.Ok(result, "Data staf berhasil ditambahkan."));
    }

    /// <summary>
    /// PUT /api/v1/staff/{id} — Memperbarui data staf dan menginvalidasi cache Redis terkait.
    /// </summary>
    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(BaseResponse<StaffDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] long id, [FromBody] UpdateStaffRequest request, CancellationToken ct)
    {
        var result = await _staffService.UpdateStaffAsync(id, request, ct);
        return Ok(BaseResponse<StaffDetailDto>.Ok(result, "Data staf berhasil diperbarui."));
    }

    /// <summary>
    /// DELETE /api/v1/staff/{id} — Soft delete staf, disable user Keycloak, revokasi token aktif instan.
    /// </summary>
    [HttpDelete("{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] long id, CancellationToken ct)
    {
        await _staffService.DeleteStaffAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/staff/{id}/link-user — Menautkan staf dengan akun Keycloak (strict 1-to-1).
    /// </summary>
    [HttpPost("{id:long}/link-user")]
    [Authorize(Roles = "ROLE_SUPERADMIN")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> LinkUser([FromRoute] long id, [FromBody] LinkUserRequest request, CancellationToken ct)
    {
        await _staffService.LinkKeycloakUserAsync(id, request.KeycloakUserId, ct);
        return Ok(BaseResponse<object>.Ok(null!, "Akun Keycloak berhasil ditautkan ke staf."));
    }

    /// <summary>
    /// POST /api/v1/staff/{id}/unlink-user — Melepaskan tautan akun Keycloak dari data staf.
    /// </summary>
    [HttpPost("{id:long}/unlink-user")]
    [Authorize(Roles = "ROLE_SUPERADMIN")]
    [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlinkUser([FromRoute] long id, CancellationToken ct)
    {
        await _staffService.UnlinkKeycloakUserAsync(id, ct);
        return Ok(BaseResponse<object>.Ok(null!, "Tautan akun Keycloak berhasil dilepaskan."));
    }

    /// <summary>
    /// POST /api/v1/staff/provision — One-Step Provisioning: buat staf + akun Keycloak sekaligus.
    /// </summary>
    [HttpPost("provision")]
    [ProducesResponseType(typeof(BaseResponse<StaffDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Provision([FromBody] ProvisionStaffRequest request, CancellationToken ct)
    {
        var result = await _staffService.ProvisionStaffWithAccountAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.IdStaf },
            BaseResponse<StaffDetailDto>.Ok(result, "Provisioning staf dan akun Keycloak berhasil."));
    }
}

// ─── Request DTOs (API Layer Only) ───────────────────────────────────────────

public sealed record LinkUserRequest(string KeycloakUserId);
