using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.WebApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public sealed class WorkflowController : ControllerBase
{
    private readonly IWorkflowEngineService _workflowEngine;

    public WorkflowController(IWorkflowEngineService workflowEngine)
    {
        _workflowEngine = workflowEngine;
    }

    /// <summary>
    /// Memuat konfigurasi langkah awal dan daftar atasan untuk pembuatan dokumen baru (Draft).
    /// </summary>
    /// <param name="request">Parameter inisiasi alur (Tipe Disposisi dan Form ID).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("start")]
    [ProducesResponseType(typeof(StartWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetStartWorkflowAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil detail status task berjalan, histori disposisi, token konkurensi, dan tombol aksi yang diizinkan untuk active user.
    /// </summary>
    /// <param name="request">Kueri pencarian task (IdTask, HashId, atau IdDokumen + IdTipeDisposisi).</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("get")]
    [ProducesResponseType(typeof(WorkflowTaskDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskDetail([FromBody] GetWorkflowTaskRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetWorkflowDetailAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengeksekusi transisi status alur kerja (Submit, Approve, Reject, Revise) dengan verifikasi token konkurensi (RowVersion).
    /// </summary>
    /// <param name="request">Payload data aksi disposisi, catatan, dan approver tujuan.</param>
    /// <param name="ct">Token pembatalan asinkron.</param>
    [HttpPost("next")]
    [ProducesResponseType(typeof(NextDisposisiResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> NextDisposisi([FromBody] NextDisposisiRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.ExecuteNextDisposisiAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil daftar approver yang valid untuk tahapan disposisi tertentu (memperhitungkan hierarki dan delegasi cuti).
    /// </summary>
    [HttpPost("approver-candidates")]
    [ProducesResponseType(typeof(List<ApproverCandidateDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetApproverCandidates([FromBody] GetApproverCandidatesRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.GetApproverCandidatesAsync(request.IdUrutanDisp, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengunggah berkas lampiran pendukung disposisi dokumen.
    /// </summary>
    [HttpPost("attachment")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(AttachmentResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadAttachment(
        [FromForm] long idTask,
        [FromForm] long? idTaskHist,
        [FromForm] string? caption,
        IFormFile file,
        CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "Berkas file tidak boleh kosong." });
        }

        await using var stream = file.OpenReadStream();
        var result = await _workflowEngine.UploadAttachmentAsync(
            idTask,
            idTaskHist,
            file.FileName,
            file.ContentType,
            stream,
            caption,
            ct);

        return Ok(result);
    }

    /// <summary>
    /// Mengunduh berkas lampiran disposisi berdasarkan ID berkas.
    /// </summary>
    [HttpGet("attachment/{id:long}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment([FromRoute] long id, CancellationToken ct)
    {
        var (fileStream, contentType, fileName) = await _workflowEngine.DownloadAttachmentAsync(id, ct);
        return File(fileStream, contentType, fileName);
    }

    /// <summary>
    /// Mendaftarkan delegasi wewenang approval sementara (Out of Office / Plt / Pjs).
    /// </summary>
    [HttpPost("delegations")]
    [ProducesResponseType(typeof(DelegationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateDelegation([FromBody] CreateDelegationRequest request, CancellationToken ct)
    {
        var result = await _workflowEngine.CreateDelegationAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// Mengambil daftar delegasi aktif yang telah dibuat oleh user yang sedang login.
    /// </summary>
    [HttpGet("delegations/my-active")]
    [ProducesResponseType(typeof(List<DelegationResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyActiveDelegations(CancellationToken ct)
    {
        var result = await _workflowEngine.GetMyActiveDelegationsAsync(ct);
        return Ok(result);
    }

    /// <summary>
    /// Mencabut atau menonaktifkan delegasi wewenang yang pernah dibuat.
    /// </summary>
    [HttpDelete("delegations/{id:long}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RevokeDelegation([FromRoute] long id, CancellationToken ct)
    {
        await _workflowEngine.RevokeDelegationAsync(id, ct);
        return NoContent();
    }
}

public record GetApproverCandidatesRequest(int IdUrutanDisp);
