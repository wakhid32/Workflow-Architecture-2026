using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Domain.Interfaces;

/// <summary>
/// Kontrak DbContext untuk layer Domain — memastikan boundary Clean Architecture
/// tanpa ketergantungan langsung pada Infrastructure.
/// </summary>
public interface IWorkflowDbContext
{
    DbSet<WmTipeDisposisi> WmTipeDisposisi { get; }
    DbSet<WmUrutanDisposisi> WmUrutanDisposisi { get; }
    DbSet<WtTasklist> WtTasklist { get; }
    DbSet<WtTasklistHist> WtTasklistHist { get; }
    DbSet<WtTasklistAssignee> WtTasklistAssignee { get; }
    DbSet<WtTasklistAttachment> WtTasklistAttachment { get; }
    DbSet<WmWorkflowDelegation> WmWorkflowDelegation { get; }
    DbSet<WtWorkflowOutbox> WtWorkflowOutbox { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
