using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.WebApi.Controllers;

/// <summary>
/// REST API Controller untuk Administrasi dan Manajemen Master Data Workflow:
/// - Tipe Disposisi (Master Modul & Jenis Alur Dokumen)
/// - Urutan Disposisi (Master Transisi State Machine & Pengaturan Tombol Aksi)
/// - Workflow Delegation (Master Delegasi Wewenang / Plt Approver)
/// </summary>
[ApiController]
[Route("api/v1/master/workflow")]
[Produces("application/json")]
public sealed class WorkflowMasterController : ControllerBase
{
    private readonly IWorkflowMasterService _masterService;

    public WorkflowMasterController(IWorkflowMasterService masterService)
    {
        _masterService = masterService;
    }

    // =========================================================================
    // 1. TIPE DISPOSISI CRUD
    // =========================================================================

    /// <summary>
    /// Mengambil seluruh master tipe disposisi workflow.
    /// </summary>
    /// <param name="onlyActive">Filter hanya yang berstatus aktif (opsional).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("tipe")]
    [ProducesResponseType(typeof(List<TipeDisposisiDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllTipeDisposisi([FromQuery] bool? onlyActive, CancellationToken ct)
    {
        var list = await _masterService.GetAllTipeDisposisiAsync(onlyActive, ct);
        return Ok(list);
    }

    /// <summary>
    /// Mengambil detail satu tipe disposisi berdasarkan ID.
    /// </summary>
    /// <param name="id">ID Tipe Disposisi (misal 101, 201, 301).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("tipe/{id:int}")]
    [ProducesResponseType(typeof(TipeDisposisiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTipeDisposisiById(int id, CancellationToken ct)
    {
        var result = await _masterService.GetTipeDisposisiByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mendaftarkan tipe disposisi / modul bisnis workflow baru.
    /// </summary>
    /// <param name="request">Payload data pendaftaran tipe disposisi.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("tipe")]
    [ProducesResponseType(typeof(TipeDisposisiDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTipeDisposisi([FromBody] CreateTipeDisposisiRequest request, CancellationToken ct)
    {
        var created = await _masterService.CreateTipeDisposisiAsync(request, ct);
        return CreatedAtAction(nameof(GetTipeDisposisiById), new { id = created.IdTipeDisposisi }, created);
    }

    /// <summary>
    /// Memperbarui informasi tipe disposisi workflow.
    /// </summary>
    /// <param name="id">ID Tipe Disposisi yang akan diperbarui.</param>
    /// <param name="request">Payload pembaruan data.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPut("tipe/{id:int}")]
    [ProducesResponseType(typeof(TipeDisposisiDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTipeDisposisi(int id, [FromBody] UpdateTipeDisposisiRequest request, CancellationToken ct)
    {
        var updated = await _masterService.UpdateTipeDisposisiAsync(id, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Menghapus atau menonaktifkan (soft delete) tipe disposisi.
    /// </summary>
    /// <param name="id">ID Tipe Disposisi.</param>
    /// <param name="hardDelete">True untuk hapus permanen dari database (jika belum ada relasi task/urutan).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpDelete("tipe/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTipeDisposisi(int id, [FromQuery] bool hardDelete = false, CancellationToken ct = default)
    {
        await _masterService.DeleteTipeDisposisiAsync(id, hardDelete, ct);
        return NoContent();
    }

    // =========================================================================
    // 2. URUTAN DISPOSISI (STATE MACHINE) CRUD
    // =========================================================================

    /// <summary>
    /// Mengambil daftar konfigurasi urutan disposisi (langkah state machine).
    /// </summary>
    /// <param name="idTipeDisposisi">Filter berdasarkan ID Tipe Disposisi (opsional).</param>
    /// <param name="idStatusDokAsal">Filter berdasarkan status dokumen asal (opsional).</param>
    /// <param name="onlyActive">Filter hanya urutan yang aktif (opsional).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("urutan")]
    [ProducesResponseType(typeof(List<UrutanDisposisiDetailDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUrutanDisposisiList(
        [FromQuery] int? idTipeDisposisi,
        [FromQuery] string? idStatusDokAsal,
        [FromQuery] bool? onlyActive,
        CancellationToken ct)
    {
        var list = await _masterService.GetUrutanDisposisiListAsync(idTipeDisposisi, idStatusDokAsal, onlyActive, ct);
        return Ok(list);
    }

    /// <summary>
    /// Mengambil detail satu konfigurasi urutan disposisi berdasarkan ID.
    /// </summary>
    /// <param name="id">ID Urutan Disposisi (PK: ID_URUTAN_DISP).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("urutan/{id:int}")]
    [ProducesResponseType(typeof(UrutanDisposisiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUrutanDisposisiById(int id, CancellationToken ct)
    {
        var result = await _masterService.GetUrutanDisposisiByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Menambahkan langkah urutan disposisi baru (transisi status, tombol, SLA, batas wewenang).
    /// </summary>
    /// <param name="request">Payload konfigurasi urutan disposisi baru.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("urutan")]
    [ProducesResponseType(typeof(UrutanDisposisiDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateUrutanDisposisi([FromBody] CreateUrutanDisposisiRequest request, CancellationToken ct)
    {
        var created = await _masterService.CreateUrutanDisposisiAsync(request, ct);
        return CreatedAtAction(nameof(GetUrutanDisposisiById), new { id = created.IdUrutanDisp }, created);
    }

    /// <summary>
    /// Memperbarui konfigurasi urutan disposisi yang sudah ada.
    /// </summary>
    /// <param name="id">ID Urutan Disposisi.</param>
    /// <param name="request">Payload pembaruan konfigurasi.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPut("urutan/{id:int}")]
    [ProducesResponseType(typeof(UrutanDisposisiDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUrutanDisposisi(int id, [FromBody] UpdateUrutanDisposisiRequest request, CancellationToken ct)
    {
        var updated = await _masterService.UpdateUrutanDisposisiAsync(id, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Menghapus atau menonaktifkan konfigurasi urutan disposisi.
    /// </summary>
    /// <param name="id">ID Urutan Disposisi.</param>
    /// <param name="hardDelete">True untuk hapus permanen dari database (jika belum berelasi dengan task berjalan).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpDelete("urutan/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUrutanDisposisi(int id, [FromQuery] bool hardDelete = false, CancellationToken ct = default)
    {
        await _masterService.DeleteUrutanDisposisiAsync(id, hardDelete, ct);
        return NoContent();
    }

    // =========================================================================
    // 3. WORKFLOW DELEGATION (PLT) CRUD
    // =========================================================================

    /// <summary>
    /// Mengambil daftar pendelegasian wewenang persetujuan (Plt).
    /// </summary>
    /// <param name="userId">Filter berdasarkan ID User pemberi atau penerima (opsional).</param>
    /// <param name="onlyActive">Filter hanya pendelegasian yang aktif (opsional).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("delegation")]
    [ProducesResponseType(typeof(List<DelegationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDelegations([FromQuery] long? userId, [FromQuery] bool? onlyActive, CancellationToken ct)
    {
        var list = await _masterService.GetDelegationsAsync(userId, onlyActive, ct);
        return Ok(list);
    }

    /// <summary>
    /// Mengambil detail satu pendelegasian wewenang berdasarkan ID.
    /// </summary>
    /// <param name="id">ID Delegasi (PK: ID_DELEGATION).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpGet("delegation/{id:long}")]
    [ProducesResponseType(typeof(DelegationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDelegationById(long id, CancellationToken ct)
    {
        var result = await _masterService.GetDelegationByIdAsync(id, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mendaftarkan pendelegasian wewenang approval baru (Plt).
    /// </summary>
    /// <param name="request">Payload data pendelegasian wewenang.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("delegation")]
    [ProducesResponseType(typeof(DelegationResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateDelegation([FromBody] CreateDelegationRequest request, CancellationToken ct)
    {
        var created = await _masterService.CreateDelegationAsync(request, ct);
        return CreatedAtAction(nameof(GetDelegationById), new { id = created.IdDelegation }, created);
    }

    /// <summary>
    /// Memperbarui pendelegasian wewenang approval yang sudah ada.
    /// </summary>
    /// <param name="id">ID Delegasi.</param>
    /// <param name="request">Payload pembaruan data pendelegasian.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPut("delegation/{id:long}")]
    [ProducesResponseType(typeof(DelegationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDelegation(long id, [FromBody] UpdateDelegationRequest request, CancellationToken ct)
    {
        var updated = await _masterService.UpdateDelegationAsync(id, request, ct);
        return Ok(updated);
    }

    /// <summary>
    /// Menghapus / mencabut pendelegasian wewenang approval.
    /// </summary>
    /// <param name="id">ID Delegasi yang akan dicabut/dihapus.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpDelete("delegation/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteDelegation(long id, CancellationToken ct)
    {
        await _masterService.DeleteDelegationAsync(id, ct);
        return NoContent();
    }
}
