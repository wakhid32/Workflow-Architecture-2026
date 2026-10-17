using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Kontrak utama Workflow Engine: start, get, next, kandidat approver, lampiran, dan delegasi.
/// </summary>
public interface IWorkflowEngineService
{
    Task<StartWorkflowResponse> GetStartWorkflowAsync(StartWorkflowRequest request, CancellationToken ct = default);
    Task<WorkflowTaskDetailResponse> GetWorkflowDetailAsync(GetWorkflowTaskRequest request, CancellationToken ct = default);
    Task<NextDisposisiResult> ExecuteNextDisposisiAsync(NextDisposisiRequest request, CancellationToken ct = default);
    Task<List<ApproverCandidateDto>> GetApproverCandidatesAsync(int idUrutanDisp, CancellationToken ct = default);
    Task<AttachmentResponseDto> UploadAttachmentAsync(long idTask, long? idTaskHist, string fileName, string contentType, Stream stream, string? caption, CancellationToken ct = default);
    Task<(Stream fileStream, string contentType, string fileName)> DownloadAttachmentAsync(long idAttachment, CancellationToken ct = default);
    Task<DelegationResponseDto> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken ct = default);
    Task<List<DelegationResponseDto>> GetMyActiveDelegationsAsync(CancellationToken ct = default);
    Task RevokeDelegationAsync(long idDelegation, CancellationToken ct = default);
}
