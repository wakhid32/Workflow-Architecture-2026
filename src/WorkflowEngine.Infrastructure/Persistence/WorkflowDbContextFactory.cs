using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace WorkflowEngine.Infrastructure.Persistence;

/// <summary>
/// Design-time DbContext factory specifically tailored for EF Core CLI migrations (PostgreSQL target).
/// Enables commands like 'dotnet ef migrations add' and 'dotnet ef database update' to execute seamlessly.
/// </summary>
public sealed class WorkflowDbContextFactory : IDesignTimeDbContextFactory<WorkflowDbContext>
{
    public WorkflowDbContext CreateDbContext(string[] args)
    {
        var currentDir = Directory.GetCurrentDirectory();
        var candidatePaths = new[]
        {
            Path.Combine(currentDir, "src", "WorkflowEngine.WebApi"),
            Path.Combine(currentDir, "..", "WorkflowEngine.WebApi"),
            currentDir
        };

        var configDir = candidatePaths.FirstOrDefault(p => File.Exists(Path.Combine(p, "appsettings.json"))) ?? currentDir;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("PostgreSqlConnection")
                               ?? configuration.GetConnectionString("DefaultConnection")
                               ?? "Host=localhost;Port=5432;Database=workflow_engine_db;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<WorkflowDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(typeof(WorkflowDbContext).Assembly.FullName);
        });

        return new WorkflowDbContext(optionsBuilder.Options);
    }
}
