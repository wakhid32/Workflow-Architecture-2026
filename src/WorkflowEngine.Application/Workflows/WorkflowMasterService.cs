using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Interfaces;

namespace WorkflowEngine.Application.Workflows;

/// <summary>
/// Implementasi lengkap layanan CRUD Master Workflow:
/// - Tipe Disposisi (Business Workflow Types)
/// - Urutan Disposisi (State Machine Transitions)
/// - Workflow Delegation (Aturan Delegasi Wewenang / Plt)
/// </summary>
public sealed class WorkflowMasterService : IWorkflowMasterService
{
    private readonly IWorkflowDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<WorkflowMasterService> _logger;

    public WorkflowMasterService(
        IWorkflowDbContext dbContext,
        ICurrentUserService currentUserService,
        ILogger<WorkflowMasterService> logger)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    // =========================================================================
    // 1. TIPE DISPOSISI CRUD
    // =========================================================================

    public async Task<List<TipeDisposisiDto>> GetAllTipeDisposisiAsync(bool? onlyActive = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WmTipeDisposisi
            .Include(t => t.UrutanDisposisiList)
            .AsNoTracking();

        if (onlyActive.HasValue)
        {
            query = query.Where(t => t.IsActive == onlyActive.Value);
        }

        var list = await query
            .OrderBy(t => t.IdTipeDisposisi)
            .ToListAsync(cancellationToken);

        return list.Select(t => new TipeDisposisiDto(
            t.IdTipeDisposisi,
            t.NamaDisposisi,
            t.Deskripsi,
            t.KodeModul,
            t.IsActive,
            t.IsReopenClosed,
            t.CreatedDate,
            t.CreatedBy,
            t.UrutanDisposisiList.Count
        )).ToList();
    }

    public async Task<TipeDisposisiDto> GetTipeDisposisiByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmTipeDisposisi
            .Include(t => t.UrutanDisposisiList)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTipeDisposisi == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmTipeDisposisi), id);
        }

        return new TipeDisposisiDto(
            entity.IdTipeDisposisi,
            entity.NamaDisposisi,
            entity.Deskripsi,
            entity.KodeModul,
            entity.IsActive,
            entity.IsReopenClosed,
            entity.CreatedDate,
            entity.CreatedBy,
            entity.UrutanDisposisiList.Count
        );
    }

    public async Task<TipeDisposisiDto> CreateTipeDisposisiAsync(CreateTipeDisposisiRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.NamaDisposisi))
        {
            throw new WorkflowException("Nama disposisi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.KodeModul))
        {
            throw new WorkflowException("Kode modul wajib diisi.");
        }

        int targetId;
        if (request.IdTipeDisposisi.HasValue && request.IdTipeDisposisi.Value > 0)
        {
            targetId = request.IdTipeDisposisi.Value;
            var exists = await _dbContext.WmTipeDisposisi
                .AnyAsync(t => t.IdTipeDisposisi == targetId, cancellationToken);

            if (exists)
            {
                throw new WorkflowException($"Tipe Disposisi dengan ID {targetId} sudah terdaftar.");
            }
        }
        else
        {
            var maxId = await _dbContext.WmTipeDisposisi
                .Select(t => (int?)t.IdTipeDisposisi)
                .MaxAsync(cancellationToken) ?? 100;

            targetId = maxId + 1;
        }

        var entity = new WmTipeDisposisi
        {
            IdTipeDisposisi = targetId,
            NamaDisposisi = request.NamaDisposisi.Trim(),
            Deskripsi = request.Deskripsi?.Trim(),
            KodeModul = request.KodeModul.Trim().ToUpperInvariant(),
            IsActive = request.IsActive,
            IsReopenClosed = request.IsReopenClosed,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = _currentUserService.UserName ?? "System"
        };

        await _dbContext.WmTipeDisposisi.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Master Tipe Disposisi created: {Id} - {Nama}", entity.IdTipeDisposisi, entity.NamaDisposisi);

        return new TipeDisposisiDto(
            entity.IdTipeDisposisi,
            entity.NamaDisposisi,
            entity.Deskripsi,
            entity.KodeModul,
            entity.IsActive,
            entity.IsReopenClosed,
            entity.CreatedDate,
            entity.CreatedBy,
            0
        );
    }

    public async Task<TipeDisposisiDto> UpdateTipeDisposisiAsync(int id, UpdateTipeDisposisiRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmTipeDisposisi
            .Include(t => t.UrutanDisposisiList)
            .FirstOrDefaultAsync(t => t.IdTipeDisposisi == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmTipeDisposisi), id);
        }

        if (string.IsNullOrWhiteSpace(request.NamaDisposisi))
        {
            throw new WorkflowException("Nama disposisi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.KodeModul))
        {
            throw new WorkflowException("Kode modul wajib diisi.");
        }

        entity.NamaDisposisi = request.NamaDisposisi.Trim();
        entity.Deskripsi = request.Deskripsi?.Trim();
        entity.KodeModul = request.KodeModul.Trim().ToUpperInvariant();
        entity.IsActive = request.IsActive;
        entity.IsReopenClosed = request.IsReopenClosed;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Master Tipe Disposisi updated: {Id} - {Nama}", entity.IdTipeDisposisi, entity.NamaDisposisi);

        return new TipeDisposisiDto(
            entity.IdTipeDisposisi,
            entity.NamaDisposisi,
            entity.Deskripsi,
            entity.KodeModul,
            entity.IsActive,
            entity.IsReopenClosed,
            entity.CreatedDate,
            entity.CreatedBy,
            entity.UrutanDisposisiList.Count
        );
    }

    public async Task<bool> DeleteTipeDisposisiAsync(int id, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmTipeDisposisi
            .FirstOrDefaultAsync(t => t.IdTipeDisposisi == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmTipeDisposisi), id);
        }

        if (hardDelete)
        {
            var hasTasks = await _dbContext.WtTasklist
                .AnyAsync(t => t.IdTipeDisposisi == id, cancellationToken);

            if (hasTasks)
            {
                throw new WorkflowException($"Tidak dapat menghapus permanen Tipe Disposisi ID {id} karena terdapat dokumen tasklist terkait. Gunakan soft-deactivate (IsActive = false).");
            }

            var hasUrutan = await _dbContext.WmUrutanDisposisi
                .AnyAsync(u => u.IdTipeDisposisi == id, cancellationToken);

            if (hasUrutan)
            {
                throw new WorkflowException($"Tidak dapat menghapus permanen Tipe Disposisi ID {id} karena masih memiliki langkah urutan disposisi. Hapus langkah urutan terlebih dahulu.");
            }

            _dbContext.WmTipeDisposisi.Remove(entity);
        }
        else
        {
            entity.IsActive = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Master Tipe Disposisi removed/deactivated: {Id} (HardDelete: {HardDelete})", id, hardDelete);

        return true;
    }

    // =========================================================================
    // 2. URUTAN DISPOSISI (STATE MACHINE) CRUD
    // =========================================================================

    public async Task<List<UrutanDisposisiDetailDto>> GetUrutanDisposisiListAsync(
        int? idTipeDisposisi = null,
        string? idStatusDokAsal = null,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WmUrutanDisposisi
            .Include(u => u.TipeDisposisi)
            .AsNoTracking();

        if (idTipeDisposisi.HasValue)
        {
            query = query.Where(u => u.IdTipeDisposisi == idTipeDisposisi.Value);
        }

        if (!string.IsNullOrWhiteSpace(idStatusDokAsal))
        {
            query = query.Where(u => u.IdStatusDokAsal == idStatusDokAsal);
        }

        if (onlyActive.HasValue)
        {
            query = query.Where(u => u.IsActive == onlyActive.Value);
        }

        var list = await query
            .OrderBy(u => u.IdTipeDisposisi)
            .ThenBy(u => u.NoUrutan)
            .ToListAsync(cancellationToken);

        return list.Select(MapToUrutanDetailDto).ToList();
    }

    public async Task<UrutanDisposisiDetailDto> GetUrutanDisposisiByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmUrutanDisposisi
            .Include(u => u.TipeDisposisi)
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmUrutanDisposisi), id);
        }

        return MapToUrutanDetailDto(entity);
    }

    public async Task<UrutanDisposisiDetailDto> CreateUrutanDisposisiAsync(CreateUrutanDisposisiRequest request, CancellationToken cancellationToken = default)
    {
        var tipeExists = await _dbContext.WmTipeDisposisi
            .AnyAsync(t => t.IdTipeDisposisi == request.IdTipeDisposisi, cancellationToken);

        if (!tipeExists)
        {
            throw new NotFoundException(nameof(WmTipeDisposisi), request.IdTipeDisposisi);
        }

        if (string.IsNullOrWhiteSpace(request.Nama))
        {
            throw new WorkflowException("Nama langkah aksi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.IdStatusDokAsal))
        {
            throw new WorkflowException("Status dokumen asal wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.IdStatusDok))
        {
            throw new WorkflowException("Status dokumen tujuan wajib diisi.");
        }

        var entity = new WmUrutanDisposisi
        {
            IdTipeDisposisi = request.IdTipeDisposisi,
            IdRoleAsal = request.IdRoleAsal,
            IdStatusDokAsal = request.IdStatusDokAsal.Trim(),
            IdRole = request.IdRole,
            IdStatusDok = request.IdStatusDok.Trim(),
            IdForm = request.IdForm,
            KodeDepartemen = request.KodeDepartemen?.Trim(),
            KodeJabatan = request.KodeJabatan?.Trim(),
            Nama = request.Nama.Trim(),
            ClassButton = string.IsNullOrWhiteSpace(request.ClassButton) ? "btn-primary" : request.ClassButton.Trim(),
            ClassIcon = string.IsNullOrWhiteSpace(request.ClassIcon) ? "fa fa-check" : request.ClassIcon.Trim(),
            ActionType = request.ActionType,
            ApprovalMode = request.ApprovalMode,
            UiFormAction = request.UiFormAction?.Trim(),
            UiFormSeting = string.IsNullOrWhiteSpace(request.UiFormSeting) ? "readonly" : request.UiFormSeting.Trim(),
            IsAwal = request.IsAwal,
            IsAkhir = request.IsAkhir,
            IsKirimAtasan = request.IsKirimAtasan,
            IsKirimPembuat = request.IsKirimPembuat,
            IsKirimSebelumnya = request.IsKirimSebelumnya,
            IsRequireAttachment = request.IsRequireAttachment,
            SlaHoursDefault = request.SlaHoursDefault,
            AmtLimitMin = request.AmtLimitMin,
            AmtLimitMax = request.AmtLimitMax,
            ConditionExpressionJson = request.ConditionExpressionJson?.Trim(),
            NoUrutan = request.NoUrutan,
            IsActive = request.IsActive
        };

        await _dbContext.WmUrutanDisposisi.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Reload to load navigation property
        await _dbContext.WmUrutanDisposisi.Entry(entity).Reference(u => u.TipeDisposisi).LoadAsync(cancellationToken);

        _logger.LogInformation("Master Urutan Disposisi created: {Id} - {Nama} (Modul {TipeId})", entity.IdUrutanDisp, entity.Nama, entity.IdTipeDisposisi);

        return MapToUrutanDetailDto(entity);
    }

    public async Task<UrutanDisposisiDetailDto> UpdateUrutanDisposisiAsync(int id, UpdateUrutanDisposisiRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmUrutanDisposisi
            .Include(u => u.TipeDisposisi)
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmUrutanDisposisi), id);
        }

        if (string.IsNullOrWhiteSpace(request.Nama))
        {
            throw new WorkflowException("Nama langkah aksi wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.IdStatusDokAsal))
        {
            throw new WorkflowException("Status dokumen asal wajib diisi.");
        }

        if (string.IsNullOrWhiteSpace(request.IdStatusDok))
        {
            throw new WorkflowException("Status dokumen tujuan wajib diisi.");
        }

        entity.IdRoleAsal = request.IdRoleAsal;
        entity.IdStatusDokAsal = request.IdStatusDokAsal.Trim();
        entity.IdRole = request.IdRole;
        entity.IdStatusDok = request.IdStatusDok.Trim();
        entity.IdForm = request.IdForm;
        entity.KodeDepartemen = request.KodeDepartemen?.Trim();
        entity.KodeJabatan = request.KodeJabatan?.Trim();
        entity.Nama = request.Nama.Trim();
        entity.ClassButton = string.IsNullOrWhiteSpace(request.ClassButton) ? "btn-primary" : request.ClassButton.Trim();
        entity.ClassIcon = string.IsNullOrWhiteSpace(request.ClassIcon) ? "fa fa-check" : request.ClassIcon.Trim();
        entity.ActionType = request.ActionType;
        entity.ApprovalMode = request.ApprovalMode;
        entity.UiFormAction = request.UiFormAction?.Trim();
        entity.UiFormSeting = string.IsNullOrWhiteSpace(request.UiFormSeting) ? "readonly" : request.UiFormSeting.Trim();
        entity.IsAwal = request.IsAwal;
        entity.IsAkhir = request.IsAkhir;
        entity.IsKirimAtasan = request.IsKirimAtasan;
        entity.IsKirimPembuat = request.IsKirimPembuat;
        entity.IsKirimSebelumnya = request.IsKirimSebelumnya;
        entity.IsRequireAttachment = request.IsRequireAttachment;
        entity.SlaHoursDefault = request.SlaHoursDefault;
        entity.AmtLimitMin = request.AmtLimitMin;
        entity.AmtLimitMax = request.AmtLimitMax;
        entity.ConditionExpressionJson = request.ConditionExpressionJson?.Trim();
        entity.NoUrutan = request.NoUrutan;
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Master Urutan Disposisi updated: {Id} - {Nama}", entity.IdUrutanDisp, entity.Nama);

        return MapToUrutanDetailDto(entity);
    }

    public async Task<bool> DeleteUrutanDisposisiAsync(int id, bool hardDelete = false, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmUrutanDisposisi
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmUrutanDisposisi), id);
        }

        if (hardDelete)
        {
            var isReferenced = await _dbContext.WtTasklist
                .AnyAsync(t => t.IdUrutanDisp == id, cancellationToken);

            if (isReferenced)
            {
                throw new WorkflowException($"Tidak dapat menghapus langkah urutan disposisi ID {id} karena sedang aktif digunakan pada dokumen tasklist. Gunakan soft delete (IsActive = false).");
            }

            _dbContext.WmUrutanDisposisi.Remove(entity);
        }
        else
        {
            entity.IsActive = false;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Master Urutan Disposisi deleted/deactivated: {Id} (HardDelete: {HardDelete})", id, hardDelete);

        return true;
    }

    // =========================================================================
    // 3. WORKFLOW DELEGATION (PLT) CRUD
    // =========================================================================

    public async Task<List<DelegationResponseDto>> GetDelegationsAsync(
        long? userId = null,
        bool? onlyActive = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WmWorkflowDelegation
            .Include(d => d.TipeDisposisi)
            .AsNoTracking();

        if (userId.HasValue)
        {
            query = query.Where(d => d.IdUserPemberi == userId.Value || d.IdUserPenerima == userId.Value);
        }

        if (onlyActive.HasValue)
        {
            query = query.Where(d => d.IsActive == onlyActive.Value);
        }

        var list = await query
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync(cancellationToken);

        return list.Select(MapToDelegationDto).ToList();
    }

    public async Task<DelegationResponseDto> GetDelegationByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmWorkflowDelegation
            .Include(d => d.TipeDisposisi)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.IdDelegation == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmWorkflowDelegation), id);
        }

        return MapToDelegationDto(entity);
    }

    public async Task<DelegationResponseDto> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken cancellationToken = default)
    {
        var currentUserId = _currentUserService.UserId;

        if (request.IdUserPenerima == currentUserId)
        {
            throw new WorkflowException("User penerima delegasi tidak boleh sama dengan user pemberi delegasi.");
        }

        if (request.TanggalSelesai < request.TanggalMulai)
        {
            throw new WorkflowException("Tanggal selesai delegasi tidak boleh lebih awal dari tanggal mulai.");
        }

        if (string.IsNullOrWhiteSpace(request.AlasanDelegasi))
        {
            throw new WorkflowException("Alasan delegasi wewenang wajib diisi.");
        }

        if (request.IdTipeDisposisi.HasValue)
        {
            var tipeExists = await _dbContext.WmTipeDisposisi
                .AnyAsync(t => t.IdTipeDisposisi == request.IdTipeDisposisi.Value, cancellationToken);

            if (!tipeExists)
            {
                throw new NotFoundException(nameof(WmTipeDisposisi), request.IdTipeDisposisi.Value);
            }
        }

        var entity = new WmWorkflowDelegation
        {
            IdUserPemberi = currentUserId,
            IdUserPenerima = request.IdUserPenerima,
            IdTipeDisposisi = request.IdTipeDisposisi,
            TanggalMulai = request.TanggalMulai,
            TanggalSelesai = request.TanggalSelesai,
            AlasanDelegasi = request.AlasanDelegasi.Trim(),
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        await _dbContext.WmWorkflowDelegation.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (entity.IdTipeDisposisi.HasValue)
        {
            await _dbContext.WmWorkflowDelegation.Entry(entity).Reference(d => d.TipeDisposisi).LoadAsync(cancellationToken);
        }

        _logger.LogInformation("Delegation created: Pemberi {Pemberi} -> Penerima {Penerima} ({Alasan})",
            entity.IdUserPemberi, entity.IdUserPenerima, entity.AlasanDelegasi);

        return MapToDelegationDto(entity);
    }

    public async Task<DelegationResponseDto> UpdateDelegationAsync(long id, UpdateDelegationRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmWorkflowDelegation
            .Include(d => d.TipeDisposisi)
            .FirstOrDefaultAsync(d => d.IdDelegation == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmWorkflowDelegation), id);
        }

        if (request.IdUserPenerima == entity.IdUserPemberi)
        {
            throw new WorkflowException("User penerima delegasi tidak boleh sama dengan user pemberi.");
        }

        if (request.TanggalSelesai < request.TanggalMulai)
        {
            throw new WorkflowException("Tanggal selesai delegasi tidak boleh lebih awal dari tanggal mulai.");
        }

        if (string.IsNullOrWhiteSpace(request.AlasanDelegasi))
        {
            throw new WorkflowException("Alasan delegasi wewenang wajib diisi.");
        }

        if (request.IdTipeDisposisi.HasValue)
        {
            var tipeExists = await _dbContext.WmTipeDisposisi
                .AnyAsync(t => t.IdTipeDisposisi == request.IdTipeDisposisi.Value, cancellationToken);

            if (!tipeExists)
            {
                throw new NotFoundException(nameof(WmTipeDisposisi), request.IdTipeDisposisi.Value);
            }
        }

        entity.IdUserPenerima = request.IdUserPenerima;
        entity.IdTipeDisposisi = request.IdTipeDisposisi;
        entity.TanggalMulai = request.TanggalMulai;
        entity.TanggalSelesai = request.TanggalSelesai;
        entity.AlasanDelegasi = request.AlasanDelegasi.Trim();
        entity.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delegation updated: ID {Id} (IsActive: {Active})", entity.IdDelegation, entity.IsActive);

        return MapToDelegationDto(entity);
    }

    public async Task<bool> DeleteDelegationAsync(long id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WmWorkflowDelegation
            .FirstOrDefaultAsync(d => d.IdDelegation == id, cancellationToken);

        if (entity == null)
        {
            throw new NotFoundException(nameof(WmWorkflowDelegation), id);
        }

        _dbContext.WmWorkflowDelegation.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Delegation deleted: ID {Id}", id);
        return true;
    }

    // =========================================================================
    // PRIVATE HELPER MAPPERS
    // =========================================================================

    private static UrutanDisposisiDetailDto MapToUrutanDetailDto(WmUrutanDisposisi u)
    {
        return new UrutanDisposisiDetailDto(
            u.IdUrutanDisp,
            u.IdTipeDisposisi,
            u.TipeDisposisi?.NamaDisposisi ?? $"Tipe {u.IdTipeDisposisi}",
            u.TipeDisposisi?.KodeModul ?? "UNKNOWN",
            u.IdRoleAsal,
            u.IdStatusDokAsal,
            u.IdRole,
            u.IdStatusDok,
            u.IdForm,
            u.KodeDepartemen,
            u.KodeJabatan,
            u.Nama,
            u.ClassButton,
            u.ClassIcon,
            u.ActionType,
            u.ApprovalMode,
            u.UiFormAction,
            u.UiFormSeting,
            u.IsAwal,
            u.IsAkhir,
            u.IsKirimAtasan,
            u.IsKirimPembuat,
            u.IsKirimSebelumnya,
            u.IsRequireAttachment,
            u.SlaHoursDefault,
            u.AmtLimitMin,
            u.AmtLimitMax,
            u.ConditionExpressionJson,
            u.NoUrutan,
            u.IsActive
        );
    }

    private static DelegationResponseDto MapToDelegationDto(WmWorkflowDelegation d)
    {
        return new DelegationResponseDto(
            d.IdDelegation,
            d.IdUserPemberi,
            $"User #{d.IdUserPemberi}",
            d.IdUserPenerima,
            $"User #{d.IdUserPenerima}",
            d.IdTipeDisposisi,
            d.TipeDisposisi?.NamaDisposisi,
            d.TanggalMulai,
            d.TanggalSelesai,
            d.AlasanDelegasi,
            d.IsActive,
            d.CreatedDate
        );
    }
}
