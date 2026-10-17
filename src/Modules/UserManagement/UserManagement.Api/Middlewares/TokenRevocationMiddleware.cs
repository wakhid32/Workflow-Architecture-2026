using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Interfaces;

namespace UserManagement.Api.Middlewares;

/// <summary>
/// Middleware pengecekan token blacklist (revocation).
/// Memeriksa apakah JTI dari access token ada di Redis blacklist.
/// Operasi sub-millisecond — hanya lookup key sederhana.
/// Sesuai RFC 7009 (Token Revocation) dan SECURITY.md Section 8.2.
/// </summary>
public sealed class TokenRevocationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRevocationMiddleware> _logger;

    public TokenRevocationMiddleware(RequestDelegate next, ILogger<TokenRevocationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklist)
    {
        // Hanya periksa jika request memiliki Authorization header
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            var token = authHeader["Bearer ".Length..].Trim();

            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    var jti = jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                    if (!string.IsNullOrEmpty(jti))
                    {
                        var isBlacklisted = await tokenBlacklist.IsTokenBlacklistedAsync(jti, context.RequestAborted);

                        if (isBlacklisted)
                        {
                            _logger.LogWarning(
                                "Token yang sudah dicabut (revoked) terdeteksi: JTI={Jti}, IP={ClientIp}",
                                jti, context.Connection.RemoteIpAddress);

                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            context.Response.ContentType = "application/json";
                            await context.Response.WriteAsJsonAsync(new
                            {
                                type = "https://tools.ietf.org/html/rfc7009",
                                title = "Token Dicabut",
                                status = 401,
                                detail = "Token akses ini telah dicabut (revoked). Silakan login kembali untuk mendapatkan token baru."
                            });
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Jangan blokir request jika Redis down — graceful degradation
                _logger.LogWarning(ex, "Gagal memeriksa token blacklist. Request tetap dilanjutkan (graceful degradation).");
            }
        }

        await _next(context);
    }
}
