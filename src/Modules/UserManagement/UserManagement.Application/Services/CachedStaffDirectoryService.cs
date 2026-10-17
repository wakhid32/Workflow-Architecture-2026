using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Interfaces;

namespace UserManagement.Application.Services;

/// <summary>
/// Implementasi IStaffDirectoryService dengan pola Cache-Aside (L2 Distributed Cache).
/// Membaca data staf dari security_identity_db, melayani pembacaan dari Redis cache
/// dengan TTL 30 menit, dan invalidasi otomatis saat mutasi staf.
/// </summary>
public sealed class CachedStaffDirectoryService : IStaffDirectoryService
{
    private readonly IUserManagementDbContext _dbContext;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedStaffDirectoryService> _logger;

    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromMinutes(30)
    };

    private const string CachePrefix = "cache:staff:";

    public CachedStaffDirectoryService(
        IUserManagementDbContext dbContext,
        IDistributedCache cache,
        ILogger<CachedStaffDirectoryService> logger)
    {
        _dbContext = dbContext;
        _cache = cache;
        _logger = logger;
    }

    public async Task<StaffSummaryDto?> GetStaffByIdAsync(long staffId, CancellationToken ct)
    {
        var cacheKey = $"{CachePrefix}{staffId}";

        // Try cache first
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<StaffSummaryDto>(cached);
        }

        // Cache miss — query database
        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .Where(s => s.IdStaf == staffId && s.IsActive)
            .Select(s => new StaffSummaryDto(
                s.IdStaf, s.Nip, s.NamaLengkap,
                s.KodeDepartemen, s.KodeJabatan,
                s.IdAtasanLangsung, s.IsActive))
            .FirstOrDefaultAsync(ct);

        if (staff is not null)
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(staff),
                CacheOptions,
                ct);
        }

        return staff;
    }

    public async Task<StaffSummaryDto?> GetImmediateSupervisorAsync(long staffId, CancellationToken ct)
    {
        // Get the staff record to find supervisor ID
        var staff = await GetStaffByIdAsync(staffId, ct);
        if (staff?.IdAtasanLangsung is null)
            return null;

        return await GetStaffByIdAsync(staff.IdAtasanLangsung.Value, ct);
    }

    public async Task<IReadOnlyList<StaffSummaryDto>> GetStaffByDepartmentAndPositionAsync(
        string departmentCode, string positionCode, CancellationToken ct)
    {
        var cacheKey = $"{CachePrefix}dept:{departmentCode}:{positionCode}";

        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            return JsonSerializer.Deserialize<List<StaffSummaryDto>>(cached) ?? [];
        }

        var staffList = await _dbContext.MStaf
            .AsNoTracking()
            .Where(s => s.KodeDepartemen == departmentCode
                     && s.KodeJabatan == positionCode
                     && s.IsActive)
            .Select(s => new StaffSummaryDto(
                s.IdStaf, s.Nip, s.NamaLengkap,
                s.KodeDepartemen, s.KodeJabatan,
                s.IdAtasanLangsung, s.IsActive))
            .ToListAsync(ct);

        if (staffList.Count > 0)
        {
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(staffList),
                CacheOptions,
                ct);
        }

        return staffList.AsReadOnly();
    }

    /// <summary>
    /// Invalidasi cache staf. Dipanggil setelah operasi mutasi (POST/PUT/DELETE) pada staf.
    /// </summary>
    public async Task InvalidateCacheAsync(long staffId, string? departmentCode, string? positionCode, CancellationToken ct)
    {
        try
        {
            await _cache.RemoveAsync($"{CachePrefix}{staffId}", ct);

            if (!string.IsNullOrEmpty(departmentCode) && !string.IsNullOrEmpty(positionCode))
            {
                await _cache.RemoveAsync($"{CachePrefix}dept:{departmentCode}:{positionCode}", ct);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Gagal menginvalidasi cache staf ID={StaffId}. Cache akan expire secara natural.", staffId);
        }
    }
}
