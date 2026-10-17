using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using UserManagement.Api.Middlewares;
using UserManagement.Infrastructure;
using UserManagement.Infrastructure.Keycloak;

namespace UserManagement.Api.Extensions;

/// <summary>
/// Extension method tunggal untuk mendaftarkan seluruh modul UserManagement
/// pada host application (Program.cs). Satu baris kode:
///   builder.Services.AddUserManagementModule(builder.Configuration);
/// Sesuai spesifikasi SECURITY.md Section 11.
/// </summary>
public static class UserManagementModuleExtensions
{
    /// <summary>
    /// Mendaftarkan Controllers, Middlewares, Polly Pipeline, Redis Cache,
    /// EF DbContext, JWT Bearer Authentication, Rate Limiter, dan semua service.
    /// </summary>
    public static IServiceCollection AddUserManagementModule(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ─── 1. Infrastructure Services (DbContext, Keycloak, Redis, Polly) ──
        services.AddUserManagementInfrastructure(configuration);

        // ─── Register Module Controllers Assembly ────────────────────────────
        services.AddControllers().AddApplicationPart(typeof(UserManagementModuleExtensions).Assembly);

        // ─── 2. JWT Bearer Authentication ────────────────────────────────────
        var keycloakOptions = new KeycloakOptions();
        configuration.GetSection(KeycloakOptions.SectionName).Bind(keycloakOptions);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.Authority = keycloakOptions.ValidIssuer;
            options.MetadataAddress = keycloakOptions.MetadataAddress;
            options.RequireHttpsMetadata = false; // Izinkan HTTP untuk development
            options.SaveToken = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = keycloakOptions.ValidIssuer,
                ValidateAudience = false, // Keycloak menggunakan azp, bukan aud
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = "preferred_username",
                RoleClaimType = "realm_access"
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
                {
                    // Transform realm_access.roles ke standar role claims
                    var realmAccessClaim = context.Principal?.FindFirst("realm_access")?.Value;
                    if (!string.IsNullOrEmpty(realmAccessClaim))
                    {
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(realmAccessClaim);
                            if (doc.RootElement.TryGetProperty("roles", out var rolesElement))
                            {
                                var identity = context.Principal?.Identity as System.Security.Claims.ClaimsIdentity;
                                foreach (var role in rolesElement.EnumerateArray())
                                {
                                    var roleName = role.GetString();
                                    if (!string.IsNullOrEmpty(roleName))
                                    {
                                        identity?.AddClaim(new System.Security.Claims.Claim(
                                            System.Security.Claims.ClaimTypes.Role, roleName));
                                    }
                                }
                            }
                        }
                        catch
                        {
                            // Silently ignore parse failures
                        }
                    }

                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    // JWKS In-Memory Cache auto-refresh on Key ID mismatch
                    if (context.Exception is SecurityTokenSignatureKeyNotFoundException)
                    {
                        context.Options.ConfigurationManager?.RequestRefresh();
                    }
                    return Task.CompletedTask;
                }
            };
        });

        services.AddAuthorization();

        // ─── 3. Rate Limiting (Anti-Brute-Force) ────────────────────────────
        services.AddRateLimiter(options =>
        {
            // Login: 5 req/menit/IP
            options.AddSlidingWindowLimiter("AuthLoginPolicy", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Register: 3 req/jam/IP
            options.AddSlidingWindowLimiter("AuthRegisterPolicy", opt =>
            {
                opt.PermitLimit = 3;
                opt.Window = TimeSpan.FromHours(1);
                opt.SegmentsPerWindow = 12;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Refresh Token: 10 req/menit
            options.AddSlidingWindowLimiter("AuthRefreshPolicy", opt =>
            {
                opt.PermitLimit = 10;
                opt.Window = TimeSpan.FromMinutes(1);
                opt.SegmentsPerWindow = 6;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Forgot Password: 3 req/15 menit
            options.AddSlidingWindowLimiter("AuthForgotPasswordPolicy", opt =>
            {
                opt.PermitLimit = 3;
                opt.Window = TimeSpan.FromMinutes(15);
                opt.SegmentsPerWindow = 5;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            // Change Password: 5 req/15 menit
            options.AddSlidingWindowLimiter("AuthChangePasswordPolicy", opt =>
            {
                opt.PermitLimit = 5;
                opt.Window = TimeSpan.FromMinutes(15);
                opt.SegmentsPerWindow = 5;
                opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                opt.QueueLimit = 0;
            });

            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        });

        return services;
    }

    /// <summary>
    /// Extension method pipeline middleware:
    ///   app.UseSecurityHeaders();
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
        => app.UseMiddleware<SecurityHeadersMiddleware>();

    /// <summary>
    /// Extension method pipeline middleware:
    ///   app.UseCorrelationId();
    /// </summary>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
        => app.UseMiddleware<CorrelationIdMiddleware>();

    /// <summary>
    /// Extension method pipeline middleware:
    ///   app.UseTokenRevocation();
    /// </summary>
    public static IApplicationBuilder UseTokenRevocation(this IApplicationBuilder app)
        => app.UseMiddleware<TokenRevocationMiddleware>();
}
