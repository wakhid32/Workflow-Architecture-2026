using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Resolusi konteks pengguna saat ini dari HTTP request headers / JWT Claims.
/// Mendukung header kustom X-User-Id / X-User-Name untuk kemudahan integrasi API gateway.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public long UserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return 1; // Default fallback for background services

            // Check custom header first
            if (context.Request.Headers.TryGetValue("X-User-Id", out var headerVal) &&
                long.TryParse(headerVal, out var parsedHeaderId))
            {
                return parsedHeaderId;
            }

            // Check Claims
            var claim = context.User.FindFirst(ClaimTypes.NameIdentifier) ??
                        context.User.FindFirst("sub") ??
                        context.User.FindFirst("uid");

            if (claim != null && long.TryParse(claim.Value, out var parsedClaimId))
            {
                return parsedClaimId;
            }

            return 1; // Default demo user
        }
    }

    public string UserName
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "System";

            if (context.Request.Headers.TryGetValue("X-User-Name", out var headerVal) &&
                !string.IsNullOrWhiteSpace(headerVal))
            {
                return headerVal.ToString();
            }

            return context.User.Identity?.Name ?? "User #" + UserId;
        }
    }

    public string? UserNpk
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("X-User-Npk", out var headerVal))
            {
                return headerVal.ToString();
            }
            return context?.User.FindFirst("npk")?.Value;
        }
    }

    public string? UserRole
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context != null && context.Request.Headers.TryGetValue("X-User-Role", out var headerVal))
            {
                return headerVal.ToString();
            }
            return context?.User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }

    public string? ClientIpAddress
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return "127.0.0.1";

            if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded))
            {
                return forwarded.ToString().Split(',')[0].Trim();
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        }
    }

    public string? UserAgent => _httpContextAccessor.HttpContext?.Request.Headers["User-Agent"].ToString();

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? true;
}
