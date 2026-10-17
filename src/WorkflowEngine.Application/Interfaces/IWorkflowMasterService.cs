using WorkflowEngine.Application.Workflows.DTOs;

namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Service contract for Master Workflow CRUD operations:
/// - Tipe Disposisi (Business Workflow Types / Modules)
/// - Urutan Disposisi (State Machine Action Transitions & Configuration)
/// - Workflow Delegation (Plt / Acting Official Delegation Rules)
/// </summary>
public interface IWorkflowMasterService
{
    // ==========================================
    // TIPE DISPOSISI
    // ==========================================
    Task<List<TipeDisposisiDto>> GetAllTipeDisposisiAsync(bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<TipeDisposisiDto> GetTipeDisposisiByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<TipeDisposisiDto> CreateTipeDisposisiAsync(CreateTipeDisposisiRequest request, CancellationToken cancellationToken = default);
    Task<TipeDisposisiDto> UpdateTipeDisposisiAsync(int id, UpdateTipeDisposisiRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteTipeDisposisiAsync(int id, bool hardDelete = false, CancellationToken cancellationToken = default);

    // ==========================================
    // URUTAN DISPOSISI (STATE MACHINE)
    // ==========================================
    Task<List<UrutanDisposisiDetailDto>> GetUrutanDisposisiListAsync(int? idTipeDisposisi = null, string? idStatusDokAsal = null, bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<UrutanDisposisiDetailDto> GetUrutanDisposisiByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<UrutanDisposisiDetailDto> CreateUrutanDisposisiAsync(CreateUrutanDisposisiRequest request, CancellationToken cancellationToken = default);
    Task<UrutanDisposisiDetailDto> UpdateUrutanDisposisiAsync(int id, UpdateUrutanDisposisiRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteUrutanDisposisiAsync(int id, bool hardDelete = false, CancellationToken cancellationToken = default);

    // ==========================================
    // WORKFLOW DELEGATION (PLT)
    // ==========================================
    Task<List<DelegationResponseDto>> GetDelegationsAsync(long? userId = null, bool? onlyActive = null, CancellationToken cancellationToken = default);
    Task<DelegationResponseDto> GetDelegationByIdAsync(long id, CancellationToken cancellationToken = default);
    Task<DelegationResponseDto> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken cancellationToken = default);
    Task<DelegationResponseDto> UpdateDelegationAsync(long id, UpdateDelegationRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDelegationAsync(long id, CancellationToken cancellationToken = default);
}
