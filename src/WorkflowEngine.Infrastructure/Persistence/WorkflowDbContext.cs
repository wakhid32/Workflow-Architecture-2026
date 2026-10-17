using System.Reflection;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Interfaces;

namespace WorkflowEngine.Infrastructure.Persistence;

public sealed class WorkflowDbContext : DbContext, IWorkflowDbContext
{
    public WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
        : base(options)
    {
    }

    public DbSet<WmTipeDisposisi> WmTipeDisposisi => Set<WmTipeDisposisi>();
    public DbSet<WmUrutanDisposisi> WmUrutanDisposisi => Set<WmUrutanDisposisi>();
    public DbSet<WtTasklist> WtTasklist => Set<WtTasklist>();
    public DbSet<WtTasklistHist> WtTasklistHist => Set<WtTasklistHist>();
    public DbSet<WtTasklistAssignee> WtTasklistAssignee => Set<WtTasklistAssignee>();
    public DbSet<WtTasklistAttachment> WtTasklistAttachment => Set<WtTasklistAttachment>();
    public DbSet<WmWorkflowDelegation> WmWorkflowDelegation => Set<WmWorkflowDelegation>();
    public DbSet<WtWorkflowOutbox> WtWorkflowOutbox => Set<WtWorkflowOutbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker.Entries<WtTasklist>();
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedDate = now;
                entry.Entity.UpdatedDate = now;
                if (entry.Entity.RowVersion == null || entry.Entity.RowVersion.Length == 0)
                {
                    entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedDate = now;
                entry.Entity.RowVersion = Guid.NewGuid().ToByteArray();
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
