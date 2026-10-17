using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Interfaces;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Persistence.Seed;

namespace WorkflowEngine.WebApi.Controllers;

[ApiController]
[Route("api/v1/dev")]
[Produces("application/json")]
public sealed class DevSimulationController : ControllerBase
{
    private readonly IWorkflowDbContext _context;

    public DevSimulationController(IWorkflowDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Dashboard ringkasan engine: jumlah tasklist aktif, outbox pending, delegasi aktif, dan modul terdaftar.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var totalTipe = await _context.WmTipeDisposisi.CountAsync(ct);
        var totalUrutan = await _context.WmUrutanDisposisi.CountAsync(ct);
        var activeTasks = await _context.WtTasklist
            .Where(t => t.TglDispAkhir == null)
            .OrderByDescending(t => t.CreatedDate)
            .Take(10)
            .Select(t => new
            {
                t.IdTask,
                t.HashId,
                t.NoDokumen,
                t.IdStatusDok,
                t.ApprovalStatus,
                t.IdUserAsal,
                t.IdUserTujuan,
                t.DueDate,
                t.IsOverdue
            })
            .ToListAsync(ct);

        var pendingOutbox = await _context.WtWorkflowOutbox
            .Where(o => o.Status == Domain.Enums.OutboxStatus.Pending)
            .OrderByDescending(o => o.CreatedDate)
            .Take(10)
            .ToListAsync(ct);

        var activeDelegations = await _context.WmWorkflowDelegation
            .Where(d => d.IsActive)
            .ToListAsync(ct);

        return Ok(new
        {
            SystemTimeUtc = DateTime.UtcNow,
            MasterData = new { TotalModules = totalTipe, TotalRules = totalUrutan },
            RecentActiveTasks = activeTasks,
            PendingOutboxCount = pendingOutbox.Count,
            PendingOutboxEvents = pendingOutbox,
            ActiveDelegations = activeDelegations
        });
    }

    /// <summary>
    /// Menjalankan ulang inisialisasi master data workflow default.
    /// </summary>
    [HttpPost("seed")]
    public async Task<IActionResult> TriggerSeed()
    {
        if (_context is WorkflowDbContext dbContext)
        {
            await WorkflowDbContextSeed.SeedDefaultWorkflowsAsync(dbContext);
            return Ok(new { message = "Master workflow seed successfully executed." });
        }

        return BadRequest(new { message = "DbContext is not an instance of WorkflowDbContext" });
    }
}
