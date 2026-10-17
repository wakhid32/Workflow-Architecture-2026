using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Domain.Interfaces;

namespace WorkflowEngine.Application.Workflows;

/// <summary>
/// Core Enterprise Workflow Engine implementation.
/// Menangani inisiasi alur, evaluasi state-machine, pelacakan SLA,
/// optimistik konkurensi, delegasi wewenang, dan outbox pattern.
/// </summary>
public sealed class WorkflowEngineService : IWorkflowEngineService
{
    private readonly IWorkflowDbContext _context;
    private readonly IApproverResolutionStrategy _approverStrategy;
    private readonly ICurrentUserService _currentUser;
    private readonly IFileStorageService _fileStorage;

    public WorkflowEngineService(
        IWorkflowDbContext context,
        IApproverResolutionStrategy approverStrategy,
        ICurrentUserService currentUser,
        IFileStorageService fileStorage)
    {
        _context = context;
        _approverStrategy = approverStrategy;
        _currentUser = currentUser;
        _fileStorage = fileStorage;
    }

    public async Task<StartWorkflowResponse> GetStartWorkflowAsync(StartWorkflowRequest request, CancellationToken ct = default)
    {
        var tipe = await _context.WmTipeDisposisi
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.IdTipeDisposisi == request.TipeDisposisi && t.IsActive, ct)
            ?? throw new NotFoundException(nameof(WmTipeDisposisi), request.TipeDisposisi);

        var urutanAwalList = await _context.WmUrutanDisposisi
            .AsNoTracking()
            .Where(u => u.IdTipeDisposisi == request.TipeDisposisi && u.IsAwal && u.IsActive)
            .OrderBy(u => u.NoUrutan)
            .ToListAsync(ct);

        if (urutanAwalList.Count == 0)
        {
            throw new WorkflowException($"Konfigurasi langkah awal alur kerja untuk Tipe {request.TipeDisposisi} ({tipe.NamaDisposisi}) tidak ditemukan.");
        }

        var buttons = urutanAwalList.Select(u => new WorkflowButtonDto(
            u.IdUrutanDisp,
            u.Nama,
            u.ClassButton,
            u.ClassIcon,
            u.ActionType.ToString().ToUpperInvariant(),
            u.UiFormAction,
            u.UiFormSeting,
            u.IsAkhir,
            u.IsKirimAtasan,
            u.IsKirimPembuat,
            u.IsKirimSebelumnya,
            u.IsRequireAttachment,
            u.SlaHoursDefault,
            u.AmtLimitMin,
            u.AmtLimitMax,
            u.ApprovalMode.ToString().ToUpperInvariant()
        )).ToList();

        var firstUrutan = urutanAwalList[0];
        var candidates = await _approverStrategy.ResolveCandidatesForStartAsync(
            _currentUser.UserId,
            firstUrutan.IdUrutanDisp,
            ct);

        return new StartWorkflowResponse(
            TipeDisposisi: request.TipeDisposisi,
            NamaDisposisi: tipe.NamaDisposisi,
            StatusDokumenAwal: firstUrutan.IdStatusDokAsal,
            UiFormSeting: firstUrutan.UiFormSeting,
            ActionButtons: buttons,
            ApproverCandidates: candidates
        );
    }

    public async Task<WorkflowTaskDetailResponse> GetWorkflowDetailAsync(GetWorkflowTaskRequest request, CancellationToken ct = default)
    {
        var query = _context.WtTasklist
            .Include(t => t.TipeDisposisi)
            .Include(t => t.UrutanDisposisi)
            .Include(t => t.Histories)
            .Include(t => t.Attachments)
            .Include(t => t.Assignees)
            .AsNoTracking();

        WtTasklist? task = null;

        if (request.IdTask.HasValue && request.IdTask.Value > 0)
        {
            task = await query.FirstOrDefaultAsync(t => t.IdTask == request.IdTask.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.HashId))
        {
            task = await query.FirstOrDefaultAsync(t => t.HashId == request.HashId, ct);
        }
        else if (!string.IsNullOrWhiteSpace(request.IdDokumen) && request.IdTipeDisposisi.HasValue)
        {
            task = await query.FirstOrDefaultAsync(t => t.IdDokumen == request.IdDokumen && t.IdTipeDisposisi == request.IdTipeDisposisi.Value, ct);
        }

        if (task == null)
        {
            throw new NotFoundException(nameof(WtTasklist), request.IdTask?.ToString() ?? request.HashId ?? request.IdDokumen ?? "N/A");
        }

        var currentUserId = _currentUser.UserId;

        // Evaluasi apakah user memiliki wewenang approval
        bool isDirectPic = task.IdUserTujuan == currentUserId;
        bool isAssignee = task.Assignees.Any(a => a.IdUser == currentUserId && a.StatusApproval == AssigneeApprovalStatus.Pending);
        bool isDelegated = await _approverStrategy.HasApprovalAuthorityAsync(currentUserId, task.IdUserTujuan, task.IdTipeDisposisi, ct);

        bool hasApprovalAuthority = (isDirectPic || isAssignee || isDelegated) && task.TglDispAkhir == null && task.ApprovalStatus == ApprovalStatus.InProgress;

        var permittedButtons = new List<WorkflowButtonDto>();
        string formSetting = "readonly";

        if (hasApprovalAuthority)
        {
            var urutanList = await _context.WmUrutanDisposisi
                .AsNoTracking()
                .Where(u => u.IdTipeDisposisi == task.IdTipeDisposisi
                         && u.IdStatusDokAsal == task.IdStatusDok
                         && u.IsActive)
                .OrderBy(u => u.NoUrutan)
                .ToListAsync(ct);

            permittedButtons = urutanList.Select(u => new WorkflowButtonDto(
                u.IdUrutanDisp,
                u.Nama,
                u.ClassButton,
                u.ClassIcon,
                u.ActionType.ToString().ToUpperInvariant(),
                u.UiFormAction,
                u.UiFormSeting,
                u.IsAkhir,
                u.IsKirimAtasan,
                u.IsKirimPembuat,
                u.IsKirimSebelumnya,
                u.IsRequireAttachment,
                u.SlaHoursDefault,
                u.AmtLimitMin,
                u.AmtLimitMax,
                u.ApprovalMode.ToString().ToUpperInvariant()
            )).ToList();

            if (permittedButtons.Count > 0)
            {
                formSetting = permittedButtons[0].UiFormSeting;
            }
        }

        var allAttachments = task.Attachments
            .Select(a => new AttachmentSummaryDto(
                a.IdAttachment,
                a.IdTask,
                a.IdTaskHist,
                a.FileName,
                a.FileExtension,
                a.ContentType,
                a.FileSizeBytes,
                a.Caption,
                a.UploadedDate,
                a.UploadedBy,
                $"/api/v1/workflow/attachment/{a.IdAttachment}"
            )).ToList();

        var auditHistories = task.Histories
            .OrderByDescending(h => h.TglTaskAkhir)
            .Select(h => new WorkflowAuditHistoryDto(
                h.IdTaskHist,
                h.ActionTaken.ToString().ToUpperInvariant(),
                h.IdUserAsal,
                $"User #{h.IdUserAsal}",
                h.IdUserTujuan,
                $"User #{h.IdUserTujuan}",
                h.IdUserEksekutor,
                $"User #{h.IdUserEksekutor}",
                h.IdStatusDok,
                h.TglTaskMulai,
                h.TglTaskAkhir,
                h.DurationMinutes,
                h.TaskInstanceJson,
                h.TotalAttachments,
                h.ClientIp,
                h.UserAgent,
                allAttachments.Where(att => att.IdTaskHist == h.IdTaskHist).ToList()
            )).ToList();

        var assignees = task.Assignees
            .Select(a => new WorkflowAssigneeDto(
                a.IdAssignee,
                a.IdTask,
                a.IdUrutanDisp,
                a.IdUser,
                $"User #{a.IdUser}",
                a.StatusApproval.ToString().ToUpperInvariant(),
                a.TglRespon,
                a.Catatan,
                a.IsDelegated,
                a.IdDelegatedFrom
            )).ToList();

        return new WorkflowTaskDetailResponse(
            IdTask: task.IdTask,
            HashId: task.HashId,
            IdDokumen: task.IdDokumen,
            NoDokumen: task.NoDokumen,
            IdForm: task.IdForm,
            IdTipeDisposisi: task.IdTipeDisposisi,
            NamaTipeDisposisi: task.TipeDisposisi.NamaDisposisi,
            KodeModul: task.TipeDisposisi.KodeModul,
            StatusDokumen: task.IdStatusDok,
            ApprovalStatus: task.ApprovalStatus.ToString().ToUpperInvariant(),
            Priority: task.Priority.ToString(),
            IdUserAsal: task.IdUserAsal,
            NamaUserAsal: $"User #{task.IdUserAsal}",
            IdUserTujuan: task.IdUserTujuan,
            NamaUserTujuan: $"User #{task.IdUserTujuan}",
            PrevIdUser: task.PrevIdUser,
            IsActivePic: hasApprovalAuthority,
            UiFormSeting: formSetting,
            TanggalMulai: task.TglDispMulai,
            TanggalSelesai: task.TglDispAkhir,
            DueDate: task.DueDate,
            SlaHours: task.SlaHours,
            IsOverdue: task.IsOverdue,
            CatatanTerakhir: task.TaskInstanceJson,
            RowVersion: task.RowVersion,
            PermittedButtons: permittedButtons,
            AuditHistories: auditHistories,
            Assignees: assignees,
            ActiveAttachments: allAttachments
        );
    }

    public async Task<NextDisposisiResult> ExecuteNextDisposisiAsync(NextDisposisiRequest request, CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        // 1. Validasi Tombol Transisi
        var urutan = await _context.WmUrutanDisposisi
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == request.IdUrutanDisposisi && u.IsActive, ct)
            ?? throw new WorkflowException("Konfigurasi transisi alur kerja tidak valid atau sudah tidak aktif.");

        // 2. Validasi Batas Nominal Kewenangan (Matrix of Authority)
        if (urutan.AmtLimitMax > 0 && (request.DocumentAmount < urutan.AmtLimitMin || request.DocumentAmount > urutan.AmtLimitMax))
        {
            throw new WorkflowException($"Nilai dokumen (Rp {request.DocumentAmount:N0}) di luar batas kewenangan nominal tombol ini (Min: Rp {urutan.AmtLimitMin:N0}, Max: Rp {urutan.AmtLimitMax:N0}).");
        }

        // 3. Validasi Berkas Lampiran Wajib
        if (urutan.IsRequireAttachment && (request.UploadedAttachmentIds == null || request.UploadedAttachmentIds.Count == 0))
        {
            throw new WorkflowException($"Langkah \"{urutan.Nama}\" mewajibkan pengunggahan berkas lampiran pendukung.");
        }

        // 4. Resolusi Target PIC
        long targetUserId = 0;
        List<long> parallelAssigneeIds = [];

        if (urutan.IsAkhir)
        {
            targetUserId = currentUserId; // Dokumen selesai/ditutup
        }
        else if (urutan.IsKirimPembuat)
        {
            var initialHist = await _context.WtTasklistHist
                .Where(h => h.IdTask == (request.IdTask ?? 0))
                .OrderBy(h => h.TglTaskMulai)
                .FirstOrDefaultAsync(ct);

            targetUserId = initialHist?.IdUserAsal ?? currentUserId;
        }
        else if (urutan.IsKirimSebelumnya)
        {
            var prevHist = await _context.WtTasklistHist
                .Where(h => h.IdTask == (request.IdTask ?? 0))
                .OrderByDescending(h => h.TglTaskAkhir)
                .FirstOrDefaultAsync(ct);

            targetUserId = prevHist?.IdUserEksekutor ?? prevHist?.IdUserAsal ?? currentUserId;
        }
        else if (urutan.ApprovalMode == ApprovalMode.AnyParallel || urutan.ApprovalMode == ApprovalMode.AllParallel)
        {
            if (request.MultiApproverIds != null && request.MultiApproverIds.Count > 0)
            {
                parallelAssigneeIds = request.MultiApproverIds.Distinct().ToList();
                targetUserId = parallelAssigneeIds[0];
            }
            else if (request.SelectedApproverId.HasValue && request.SelectedApproverId.Value > 0)
            {
                targetUserId = request.SelectedApproverId.Value;
                parallelAssigneeIds.Add(targetUserId);
            }
            else
            {
                throw new WorkflowException("Daftar approver paralel wajib ditentukan.");
            }
        }
        else if (request.SelectedApproverId.HasValue && request.SelectedApproverId.Value > 0)
        {
            targetUserId = request.SelectedApproverId.Value;
        }
        else
        {
            throw new WorkflowException("Pengguna approver tujuan wajib dipilih.");
        }

        WtTasklist task;
        long? createdHistId = null;

        // SKENARIO A: INISIASI DOKUMEN BARU (DRAFT -> SUBMIT)
        if (!request.IdTask.HasValue || request.IdTask.Value == 0)
        {
            task = new WtTasklist
            {
                HashId = Guid.NewGuid().ToString("N"),
                IdTipeDisposisi = urutan.IdTipeDisposisi,
                IdUrutanDisp = urutan.IdUrutanDisp,
                IdDokumen = request.IdDokumen,
                NoDokumen = request.NoDokumen,
                IdForm = request.IdForm,
                IdUserAsal = currentUserId,
                IdUserTujuan = targetUserId,
                PrevIdUser = currentUserId,
                IdStatusDok = urutan.IdStatusDok,
                ApprovalStatus = urutan.IsAkhir 
                    ? (urutan.ActionType == WorkflowActionType.Reject ? ApprovalStatus.Rejected : ApprovalStatus.Completed)
                    : ApprovalStatus.InProgress,
                Priority = TaskPriority.Normal,
                TglDispMulai = now,
                TglDispAkhir = urutan.IsAkhir ? now : null,
                SlaHours = urutan.SlaHoursDefault,
                DueDate = urutan.SlaHoursDefault.HasValue ? now.AddHours(urutan.SlaHoursDefault.Value) : null,
                TaskInstanceJson = request.CatatanDisposisi,
                CreatedDate = now,
                CreatedBy = currentUserId,
                UpdatedDate = now,
                UpdatedBy = currentUserId
            };

            _context.WtTasklist.Add(task);
            await _context.SaveChangesAsync(ct);

            // Buat history record inisiasi
            var initialHist = new WtTasklistHist
            {
                IdTask = task.IdTask,
                IdUrutanDisp = urutan.IdUrutanDisp,
                IdUserAsal = currentUserId,
                IdUserTujuan = targetUserId,
                IdUserEksekutor = currentUserId,
                IdStatusDok = urutan.IdStatusDokAsal,
                ActionTaken = urutan.ActionType,
                TglTaskMulai = now,
                TglTaskAkhir = now,
                DurationMinutes = 0,
                TaskInstanceJson = request.CatatanDisposisi,
                TotalAttachments = request.UploadedAttachmentIds?.Count ?? 0,
                ClientIp = _currentUser.ClientIpAddress,
                UserAgent = _currentUser.UserAgent
            };
            _context.WtTasklistHist.Add(initialHist);
            await _context.SaveChangesAsync(ct);
            createdHistId = initialHist.IdTaskHist;

            // Tambahkan multi-approvers jika mode paralel
            if (parallelAssigneeIds.Count > 0)
            {
                foreach (var approverId in parallelAssigneeIds)
                {
                    _context.WtTasklistAssignee.Add(new WtTasklistAssignee
                    {
                        IdTask = task.IdTask,
                        IdUrutanDisp = urutan.IdUrutanDisp,
                        IdUser = approverId,
                        StatusApproval = AssigneeApprovalStatus.Pending
                    });
                }
            }
        }
        // SKENARIO B: TRANSISI DOKUMEN AKTIF (EXISTING TASK)
        else
        {
            task = await _context.WtTasklist
                .Include(t => t.Assignees)
                .FirstOrDefaultAsync(t => t.IdTask == request.IdTask.Value, ct)
                ?? throw new NotFoundException(nameof(WtTasklist), request.IdTask.Value);

            // Validasi Hak Akses Eksekutor (Langsung, Assignee, atau Delegasi)
            bool isDirectPic = task.IdUserTujuan == currentUserId;
            bool isAssignee = task.Assignees.Any(a => a.IdUser == currentUserId && a.StatusApproval == AssigneeApprovalStatus.Pending);
            bool isDelegated = await _approverStrategy.HasApprovalAuthorityAsync(currentUserId, task.IdUserTujuan, task.IdTipeDisposisi, ct);

            if (!isDirectPic && !isAssignee && !isDelegated)
            {
                throw new ForbiddenException("Anda tidak memiliki kewenangan approval atas dokumen ini.");
            }

            // Validasi Optimistic Concurrency Token
            if (request.RowVersion != null && request.RowVersion.Length > 0 && task.RowVersion.Length > 0)
            {
                if (!task.RowVersion.SequenceEqual(request.RowVersion))
                {
                    throw new ConcurrencyException("Dokumen telah diperbarui atau disetujui oleh pengguna lain. Silakan segarkan halaman browser Anda.");
                }
            }

            // Arsip step sebelumnya ke WT_TASKLIST_HIST
            var duration = Math.Max(0, (int)(now - task.TglDispMulai).TotalMinutes);
            var history = new WtTasklistHist
            {
                IdTask = task.IdTask,
                IdUrutanDisp = task.IdUrutanDisp,
                IdUserAsal = task.IdUserAsal,
                IdUserTujuan = task.IdUserTujuan,
                IdUserEksekutor = currentUserId,
                IdStatusDok = task.IdStatusDok,
                ActionTaken = urutan.ActionType,
                TglTaskMulai = task.TglDispMulai,
                TglTaskAkhir = now,
                DurationMinutes = duration,
                TaskInstanceJson = request.CatatanDisposisi,
                TotalAttachments = request.UploadedAttachmentIds?.Count ?? 0,
                ClientIp = _currentUser.ClientIpAddress,
                UserAgent = _currentUser.UserAgent
            };
            _context.WtTasklistHist.Add(history);
            await _context.SaveChangesAsync(ct);
            createdHistId = history.IdTaskHist;

            // Update Tasklist aktif
            task.PrevIdUser = task.IdUserTujuan;
            task.IdUserAsal = currentUserId;
            task.IdUserTujuan = targetUserId;
            task.IdStatusDok = urutan.IdStatusDok;
            task.IdUrutanDisp = urutan.IdUrutanDisp;
            task.TglDispMulai = now;
            task.TaskInstanceJson = request.CatatanDisposisi;
            task.UpdatedDate = now;
            task.UpdatedBy = currentUserId;

            if (urutan.IsAkhir)
            {
                task.TglDispAkhir = now;
                task.ApprovalStatus = urutan.ActionType == WorkflowActionType.Reject
                    ? ApprovalStatus.Rejected
                    : ApprovalStatus.Completed;
            }
            else
            {
                task.ApprovalStatus = ApprovalStatus.InProgress;
                task.SlaHours = urutan.SlaHoursDefault;
                task.DueDate = urutan.SlaHoursDefault.HasValue ? now.AddHours(urutan.SlaHoursDefault.Value) : null;
                task.IsOverdue = false;
            }

            // Tambahkan assignees baru jika langkah ini paralel
            if (parallelAssigneeIds.Count > 0)
            {
                foreach (var approverId in parallelAssigneeIds)
                {
                    _context.WtTasklistAssignee.Add(new WtTasklistAssignee
                    {
                        IdTask = task.IdTask,
                        IdUrutanDisp = urutan.IdUrutanDisp,
                        IdUser = approverId,
                        StatusApproval = AssigneeApprovalStatus.Pending
                    });
                }
            }
        }

        // Tautkan berkas lampiran yang diunggah ke riwayat history
        if (createdHistId.HasValue && request.UploadedAttachmentIds != null && request.UploadedAttachmentIds.Count > 0)
        {
            var attachmentsToLink = await _context.WtTasklistAttachment
                .Where(a => request.UploadedAttachmentIds.Contains(a.IdAttachment))
                .ToListAsync(ct);

            foreach (var att in attachmentsToLink)
            {
                att.IdTaskHist = createdHistId.Value;
                att.IdTask = task.IdTask;
            }
        }

        // Catat Transactional Outbox Event
        _context.WtWorkflowOutbox.Add(new WtWorkflowOutbox
        {
            IdTask = task.IdTask,
            EventType = urutan.IsAkhir ? "WORKFLOW_COMPLETED" : "TASK_ASSIGNED",
            RecipientPayloadJson = $"{{\"TargetUserId\": {targetUserId}}}",
            MessagePayloadJson = $"{{\"DocumentNo\": \"{task.NoDokumen}\", \"Status\": \"{urutan.IdStatusDok}\", \"Action\": \"{urutan.ActionType}\", \"SenderId\": {currentUserId}}}",
            Status = OutboxStatus.Pending,
            CreatedDate = now
        });

        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException("Terjadi benturan data konkuren saat menyimpan workflow.", ex);
        }

        return new NextDisposisiResult(
            Success: true,
            IdTask: task.IdTask,
            HashId: task.HashId,
            StatusDokumenBaru: task.IdStatusDok,
            ApprovalStatus: task.ApprovalStatus.ToString().ToUpperInvariant(),
            IsCompleted: urutan.IsAkhir,
            Message: $"Disposisi alur kerja \"{urutan.Nama}\" berhasil diproses.",
            TargetUserId: targetUserId,
            Timestamp: now
        );
    }

    public async Task<List<ApproverCandidateDto>> GetApproverCandidatesAsync(int idUrutanDisp, CancellationToken ct = default)
    {
        return await _approverStrategy.ResolveCandidatesForStepAsync(idUrutanDisp, _currentUser.UserId, ct);
    }

    public async Task<AttachmentResponseDto> UploadAttachmentAsync(
        long idTask,
        long? idTaskHist,
        string fileName,
        string contentType,
        Stream stream,
        string? caption,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var (storagePath, sizeBytes) = await _fileStorage.SaveFileAsync(stream, fileName, ct);

        var attachment = new WtTasklistAttachment
        {
            IdTask = idTask,
            IdTaskHist = idTaskHist,
            FileName = fileName,
            FileExtension = ext,
            ContentType = contentType,
            FileSizeBytes = sizeBytes,
            StoragePath = storagePath,
            Caption = caption,
            UploadedDate = DateTime.UtcNow,
            UploadedBy = _currentUser.UserId
        };

        _context.WtTasklistAttachment.Add(attachment);
        await _context.SaveChangesAsync(ct);

        return new AttachmentResponseDto(
            attachment.IdAttachment,
            attachment.IdTask,
            attachment.FileName,
            attachment.ContentType,
            attachment.FileSizeBytes,
            $"/api/v1/workflow/attachment/{attachment.IdAttachment}",
            attachment.UploadedDate
        );
    }

    public async Task<(Stream fileStream, string contentType, string fileName)> DownloadAttachmentAsync(long idAttachment, CancellationToken ct = default)
    {
        var attachment = await _context.WtTasklistAttachment
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.IdAttachment == idAttachment, ct)
            ?? throw new NotFoundException(nameof(WtTasklistAttachment), idAttachment);

        var (stream, contentType, fileName) = await _fileStorage.GetFileAsync(attachment.StoragePath, ct);
        return (stream, contentType, attachment.FileName);
    }

    public async Task<DelegationResponseDto> CreateDelegationAsync(CreateDelegationRequest request, CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;

        if (request.TanggalSelesai <= request.TanggalMulai)
        {
            throw new WorkflowException("Tanggal selesai delegasi harus lebih besar dari tanggal mulai.");
        }

        var delegation = new WmWorkflowDelegation
        {
            IdUserPemberi = currentUserId,
            IdUserPenerima = request.IdUserPenerima,
            IdTipeDisposisi = request.IdTipeDisposisi,
            TanggalMulai = request.TanggalMulai.ToUniversalTime(),
            TanggalSelesai = request.TanggalSelesai.ToUniversalTime(),
            AlasanDelegasi = request.AlasanDelegasi,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = currentUserId
        };

        _context.WmWorkflowDelegation.Add(delegation);
        await _context.SaveChangesAsync(ct);

        string? namaTipe = null;
        if (request.IdTipeDisposisi.HasValue)
        {
            var tipe = await _context.WmTipeDisposisi
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.IdTipeDisposisi == request.IdTipeDisposisi.Value, ct);
            namaTipe = tipe?.NamaDisposisi;
        }

        return new DelegationResponseDto(
            delegation.IdDelegation,
            delegation.IdUserPemberi,
            $"User #{delegation.IdUserPemberi}",
            delegation.IdUserPenerima,
            $"User #{delegation.IdUserPenerima}",
            delegation.IdTipeDisposisi,
            namaTipe,
            delegation.TanggalMulai,
            delegation.TanggalSelesai,
            delegation.AlasanDelegasi,
            delegation.IsActive,
            delegation.CreatedDate
        );
    }

    public async Task<List<DelegationResponseDto>> GetMyActiveDelegationsAsync(CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;
        var now = DateTime.UtcNow;

        var list = await _context.WmWorkflowDelegation
            .Include(d => d.TipeDisposisi)
            .AsNoTracking()
            .Where(d => d.IdUserPemberi == currentUserId && d.IsActive && d.TanggalSelesai >= now)
            .OrderByDescending(d => d.CreatedDate)
            .ToListAsync(ct);

        return list.Select(d => new DelegationResponseDto(
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
        )).ToList();
    }

    public async Task RevokeDelegationAsync(long idDelegation, CancellationToken ct = default)
    {
        var currentUserId = _currentUser.UserId;
        var delegation = await _context.WmWorkflowDelegation
            .FirstOrDefaultAsync(d => d.IdDelegation == idDelegation && d.IdUserPemberi == currentUserId, ct)
            ?? throw new NotFoundException(nameof(WmWorkflowDelegation), idDelegation);

        delegation.IsActive = false;
        await _context.SaveChangesAsync(ct);
    }
}
