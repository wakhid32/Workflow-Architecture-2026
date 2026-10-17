using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Interfaces;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Services;

namespace WorkflowEngine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider", "InMemory") ?? "InMemory";
        var useInMemory = configuration.GetValue<bool>("UseInMemoryDatabase", false);

        services.AddDbContext<WorkflowDbContext>(options =>
        {
            if (useInMemory || databaseProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                options.UseInMemoryDatabase("WorkflowEngineEnterpriseDb");
            }
            else if (databaseProvider.Equals("PostgreSQL", StringComparison.OrdinalIgnoreCase) ||
                     databaseProvider.Equals("Postgres", StringComparison.OrdinalIgnoreCase))
            {
                var pgConnection = configuration.GetConnectionString("PostgreSqlConnection")
                                   ?? configuration.GetConnectionString("DefaultConnection")
                                   ?? "Host=localhost;Port=5432;Database=workflow_engine_db;Username=postgres;Password=postgres";

                options.UseNpgsql(pgConnection, npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName);
                    npgsqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorCodesToAdd: null);
                });
            }
            else // SqlServer
            {
                var sqlConnection = configuration.GetConnectionString("DefaultConnection");
                if (string.IsNullOrWhiteSpace(sqlConnection) || sqlConnection.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
                {
                    options.UseInMemoryDatabase("WorkflowEngineEnterpriseDb");
                }
                else
                {
                    options.UseSqlServer(sqlConnection, sqlOptions =>
                    {
                        sqlOptions.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName);
                        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                    });
                }
            }
        });

        services.AddScoped<IWorkflowDbContext>(provider => provider.GetRequiredService<WorkflowDbContext>());

        // User Context & Infrastructure Services
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IApproverResolutionStrategy, ApproverResolutionStrategy>();
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // SignalR Real-Time Hub
        services.AddSignalR();

        // Background Workers
        services.AddHostedService<WorkflowSlaMonitoringService>();
        services.AddHostedService<WorkflowOutboxDispatcherService>();

        return services;
    }
}
