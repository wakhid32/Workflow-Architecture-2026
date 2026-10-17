using System.Reflection;
using Microsoft.EntityFrameworkCore;
using UserManagement.Application.Interfaces;
using UserManagement.Domain.Entities;

namespace UserManagement.Infrastructure.Persistence;

/// <summary>
/// DbContext khusus untuk database security_identity_db.
/// Mengelola tabel M_STAF dan SEC_AUDIT_LOG secara terisolasi
/// dari WorkflowDbContext (database-per-domain pattern).
/// </summary>
public sealed class UserManagementDbContext : DbContext, IUserManagementDbContext
{
    public UserManagementDbContext(DbContextOptions<UserManagementDbContext> options)
        : base(options)
    {
    }

    public DbSet<MStaf> MStaf => Set<MStaf>();
    public DbSet<SecAuditLog> SecAuditLog => Set<SecAuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<MStaf>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    entry.Entity.UpdatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedDate = now;
                    break;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
