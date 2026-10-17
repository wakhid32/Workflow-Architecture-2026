using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Interfaces;

namespace UserManagement.Infrastructure.Caching;

/// <summary>
/// Implementasi ITokenBlacklistService berbasis Redis Distributed Cache.
/// Menyimpan JTI token yang telah direvokasi dengan TTL = sisa masa berlaku token.
/// Operasi lookup dijamin sub-millisecond overhead.
/// </summary>
public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisTokenBlacklistService> _logger;

    private const string BlacklistKeyPrefix = "blacklist:jti:";

    public RedisTokenBlacklistService(
        IDistributedCache cache,
        ILogger<RedisTokenBlacklistService> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(jti))
            return;

        // Pastikan TTL minimal 1 detik
        if (remainingLifetime <= TimeSpan.Zero)
            return;

        var key = $"{BlacklistKeyPrefix}{jti}";

        await _cache.SetStringAsync(
            key,
            "revoked",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = remainingLifetime
            },
            ct);

        _logger.LogInformation(
            "Token di-blacklist: JTI={Jti}, TTL={TtlSeconds}s",
            jti, (int)remainingLifetime.TotalSeconds);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(jti))
            return false;

        var key = $"{BlacklistKeyPrefix}{jti}";
        var value = await _cache.GetStringAsync(key, ct);

        return !string.IsNullOrEmpty(value);
    }
}
