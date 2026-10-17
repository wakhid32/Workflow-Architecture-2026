using Microsoft.EntityFrameworkCore;
using UserManagement.Domain.Entities;

namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak DbContext untuk database security_identity_db.
/// Memastikan layer Application hanya bergantung pada abstraksi,
/// bukan implementasi konkret Infrastructure.
/// </summary>
public interface IUserManagementDbContext
{
    DbSet<MStaf> MStaf { get; }
    DbSet<SecAuditLog> SecAuditLog { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
