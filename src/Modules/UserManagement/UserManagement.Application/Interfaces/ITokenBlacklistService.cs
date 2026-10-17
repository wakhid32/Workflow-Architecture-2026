namespace UserManagement.Application.Interfaces;

/// <summary>
/// Kontrak layanan blacklist token JWT berbasis distributed cache (Redis).
/// Sesuai RFC 7009 (Token Revocation) — menyimpan JTI yang telah dicabut
/// agar dapat diverifikasi oleh TokenRevocationMiddleware.
/// </summary>
public interface ITokenBlacklistService
{
    /// <summary>
    /// Mendaftarkan token JWT ke blacklist.
    /// Key: blacklist:jti:{jti}, TTL = sisa masa berlaku token (exp - now).
    /// </summary>
    Task BlacklistTokenAsync(string jti, TimeSpan remainingLifetime, CancellationToken ct = default);

    /// <summary>
    /// Memeriksa apakah token dengan JTI ini ada di blacklist (telah direvokasi).
    /// Operasi ini wajib sub-millisecond untuk performa middleware.
    /// </summary>
    Task<bool> IsTokenBlacklistedAsync(string jti, CancellationToken ct = default);
}
