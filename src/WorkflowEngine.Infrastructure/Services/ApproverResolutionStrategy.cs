using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Strategi resolusi hierarki approver, role-matrix organisasi,
/// dan pengalihan hak persetujuan ke pejabat pengganti (Plt/Pjs / Out of Office Delegation).
/// </summary>
public sealed class ApproverResolutionStrategy : IApproverResolutionStrategy
{
    private readonly IWorkflowDbContext _context;

    // Default enterprise candidate list for testing/demo
    private static readonly List<ApproverCandidateDto> StandardDirectory =
    [
        new ApproverCandidateDto(1, "Budi Santoso", "NPK-1001", "Staff Pemohon", "Procurement"),
        new ApproverCandidateDto(2, "Siti Rahmawati", "NPK-1002", "Section Head", "Procurement"),
        new ApproverCandidateDto(3, "Hendro Prabowo", "NPK-1003", "Department Head", "Procurement"),
        new ApproverCandidateDto(4, "Dewi Lestari", "NPK-1004", "General Manager", "Supply Chain"),
        new ApproverCandidateDto(5, "Agus Salim", "NPK-1005", "Finance Director", "Finance & Accounting"),
        new ApproverCandidateDto(6, "Rina Marlina", "NPK-1006", "Acting Officer (Plt Section Head)", "Procurement")
    ];

    public ApproverResolutionStrategy(IWorkflowDbContext context)
    {
        _context = context;
    }

    public async Task<List<ApproverCandidateDto>> ResolveCandidatesForStartAsync(long userId, int idUrutanDisp, CancellationToken ct = default)
    {
        var urutan = await _context.WmUrutanDisposisi
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == idUrutanDisp, ct);

        // Ambil kandidat atasan (misal ID 2 - Section Head jika pembuat ID 1)
        var candidates = StandardDirectory.Where(u => u.IdUser != userId).ToList();

        if (urutan != null && urutan.IdRole > 0)
        {
            // Saring kandidat sesuai target role urutan jika spesifik
            var roleFiltered = candidates.Where(c => c.IdUser == urutan.IdRole).ToList();
            if (roleFiltered.Count > 0)
            {
                candidates = roleFiltered;
            }
        }

        return await EnrichWithDelegationsAsync(candidates, urutan?.IdTipeDisposisi ?? 0, ct);
    }

    public async Task<List<ApproverCandidateDto>> ResolveCandidatesForStepAsync(int idUrutanDisp, long currentUserId, CancellationToken ct = default)
    {
        var urutan = await _context.WmUrutanDisposisi
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.IdUrutanDisp == idUrutanDisp, ct);

        var candidates = StandardDirectory.Where(u => u.IdUser != currentUserId).ToList();

        if (urutan != null && urutan.IdRole > 0)
        {
            var matched = candidates.Where(c => c.IdUser == urutan.IdRole).ToList();
            if (matched.Count > 0)
            {
                candidates = matched;
            }
        }

        return await EnrichWithDelegationsAsync(candidates, urutan?.IdTipeDisposisi ?? 0, ct);
    }

    public async Task<bool> HasApprovalAuthorityAsync(long currentUserId, long picUserId, int idTipeDisposisi, CancellationToken ct = default)
    {
        if (currentUserId == picUserId)
        {
            return true;
        }

        var now = DateTime.UtcNow;

        // Pengecekan pendelegasian wewenang aktif (Plt/Pjs / Cuti)
        return await _context.WmWorkflowDelegation
            .AsNoTracking()
            .AnyAsync(d => d.IdUserPemberi == picUserId
                        && d.IdUserPenerima == currentUserId
                        && d.IsActive
                        && now >= d.TanggalMulai
                        && now <= d.TanggalSelesai
                        && (d.IdTipeDisposisi == null || d.IdTipeDisposisi == idTipeDisposisi), ct);
    }

    private async Task<List<ApproverCandidateDto>> EnrichWithDelegationsAsync(List<ApproverCandidateDto> candidates, int idTipeDisposisi, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var candidateIds = candidates.Select(c => c.IdUser).ToList();

        var activeDelegations = await _context.WmWorkflowDelegation
            .AsNoTracking()
            .Where(d => candidateIds.Contains(d.IdUserPemberi)
                     && d.IsActive
                     && now >= d.TanggalMulai
                     && now <= d.TanggalSelesai
                     && (d.IdTipeDisposisi == null || d.IdTipeDisposisi == idTipeDisposisi))
            .ToListAsync(ct);

        var result = new List<ApproverCandidateDto>();

        foreach (var c in candidates)
        {
            var delegation = activeDelegations.FirstOrDefault(d => d.IdUserPemberi == c.IdUser);
            if (delegation != null)
            {
                var receiver = StandardDirectory.FirstOrDefault(u => u.IdUser == delegation.IdUserPenerima);
                result.Add(c with
                {
                    IsDelegated = true,
                    DelegatedFromUserId = c.IdUser,
                    DelegatedFromName = c.NamaLengkap
                });

                if (receiver != null && !result.Any(r => r.IdUser == receiver.IdUser))
                {
                    result.Add(receiver with
                    {
                        IsDelegated = true,
                        DelegatedFromUserId = c.IdUser,
                        DelegatedFromName = $"{c.NamaLengkap} (Plt/Pjs - {delegation.AlasanDelegasi})"
                    });
                }
            }
            else
            {
                result.Add(c);
            }
        }

        return result;
    }
}
