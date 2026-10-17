using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Resolusi konteks pengguna saat ini dari HTTP request headers / JWT Claims.
/// Mendukung zero-DB-hop runtime execution via enriched claims (SECURITY.md Section 7.2).
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

            // 1. Check Enriched StaffId claim
            var staffIdClaim = context.User.FindFirst("staff_id");
            if (staffIdClaim != null && long.TryParse(staffIdClaim.Value, out var parsedStaffId))
            {
                return parsedStaffId;
            }

            // 2. Check custom header
            if (context.Request.Headers.TryGetValue("X-User-Id", out var headerVal) &&
                long.TryParse(headerVal, out var parsedHeaderId))
            {
                return parsedHeaderId;
            }

            // 3. Check Standard Claims
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

    public long? StaffId => UserId;

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

            return context.User.FindFirst("preferred_username")?.Value ??
                   context.User.Identity?.Name ??
                   "User #" + UserId;
        }
    }

    public string? UserNpk => StaffNip;

    public string? StaffNip
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var nipClaim = context.User.FindFirst("staff_nip") ??
                           context.User.FindFirst("npk");
            if (nipClaim != null && !string.IsNullOrWhiteSpace(nipClaim.Value))
            {
                return nipClaim.Value;
            }

            if (context.Request.Headers.TryGetValue("X-User-Npk", out var headerVal))
            {
                return headerVal.ToString();
            }

            return null;
        }
    }

    public string? DepartmentCode
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var deptClaim = context.User.FindFirst("department_code");
            if (deptClaim != null && !string.IsNullOrWhiteSpace(deptClaim.Value))
            {
                return deptClaim.Value;
            }

            if (context.Request.Headers.TryGetValue("X-Department-Code", out var headerVal))
            {
                return headerVal.ToString();
            }

            return null;
        }
    }

    public string? PositionCode
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return null;

            var posClaim = context.User.FindFirst("position_code");
            if (posClaim != null && !string.IsNullOrWhiteSpace(posClaim.Value))
            {
                return posClaim.Value;
            }

            if (context.Request.Headers.TryGetValue("X-Position-Code", out var headerVal))
            {
                return headerVal.ToString();
            }

            return null;
        }
    }

    public string? KeycloakUserId
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User.FindFirst("sub")?.Value;
        }
    }

    public string? Email
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            return context?.User.FindFirst(ClaimTypes.Email)?.Value ??
                   context?.User.FindFirst("email")?.Value;
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
            return Roles.FirstOrDefault() ?? context?.User.FindFirst(ClaimTypes.Role)?.Value;
        }
    }

    public IReadOnlyList<string> Roles
    {
        get
        {
            var context = _httpContextAccessor.HttpContext;
            if (context == null) return [];

            return context.User.FindAll(ClaimTypes.Role)
                .Concat(context.User.FindAll("role"))
                .Select(c => c.Value)
                .Distinct()
                .ToList();
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

