using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Infrastructure.Caching;
using UserManagement.Infrastructure.Keycloak;
using UserManagement.Infrastructure.Persistence;

namespace UserManagement.Infrastructure;

/// <summary>
/// Extension method untuk mendaftarkan seluruh layanan infrastruktur UserManagement:
/// DbContext, Keycloak HTTP Client, Redis Cache, Polly Resilience Pipeline.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddUserManagementInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // ─── 1. Keycloak Options ─────────────────────────────────────────────
        services.Configure<KeycloakOptions>(configuration.GetSection(KeycloakOptions.SectionName));

        // ─── 2. UserManagement DbContext (security_identity_db) ──────────────
        var securityConnStr = configuration.GetConnectionString("SecurityDatabase");

        if (!string.IsNullOrEmpty(securityConnStr))
        {
            services.AddDbContext<UserManagementDbContext>(options =>
            {
                options.UseNpgsql(securityConnStr, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(UserManagementDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorCodesToAdd: null);
                });
            });
        }
        else
        {
            // Fallback: InMemory for development without PostgreSQL
            services.AddDbContext<UserManagementDbContext>(options =>
                options.UseInMemoryDatabase("SecurityIdentityDb"));
        }

        services.AddScoped<IUserManagementDbContext>(provider =>
            provider.GetRequiredService<UserManagementDbContext>());

        // ─── 3. Keycloak HTTP Client with Polly Resilience Pipeline ──────────
        services.AddSingleton<KeycloakHttpClient>();

        services.AddHttpClient<KeycloakHttpClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddStandardResilienceHandler(options =>
        {
            // Circuit Breaker: 50% failure ratio, 30s sampling, 30s break
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

            // Retry: 3 attempts, exponential backoff with jitter
            options.Retry.MaxRetryAttempts = 3;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;

            // Timeout: 5 seconds per attempt
            options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(5);
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        services.AddHttpClient<KeycloakAdminService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        })
        .AddStandardResilienceHandler();

        // ─── 4. Service Registrations ────────────────────────────────────────
        services.AddScoped<IKeycloakAdminService, KeycloakAdminService>();
        services.AddScoped<IKeycloakHttpClient>(sp => sp.GetRequiredService<KeycloakHttpClient>());
        services.AddScoped<IKeycloakAuthService, KeycloakAuthService>();
        services.AddScoped<IStaffManagementService, StaffManagementService>();
        services.AddScoped<IStaffDirectoryService, CachedStaffDirectoryService>();
        services.AddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();

        // ─── 5. Redis Distributed Cache ──────────────────────────────────────
        var redisConnStr = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnStr))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnStr;
                options.InstanceName = "wf:";
            });
        }
        else
        {
            // Fallback: In-memory distributed cache for development
            services.AddDistributedMemoryCache();
        }

        return services;
    }
}
