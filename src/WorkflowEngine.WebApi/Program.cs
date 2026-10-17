using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application;
using WorkflowEngine.Infrastructure;
using WorkflowEngine.Infrastructure.Hubs;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Persistence.Seed;
using WorkflowEngine.WebApi.Filters;

var builder = WebApplication.CreateBuilder(args);

// 1. Dependency Injection: Application & Infrastructure Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

// 2. Web API Controllers with Global Exception Filter
builder.Services.AddScoped<ApiExceptionFilterAttribute>();
builder.Services.AddControllers(options =>
{
    options.Filters.Add<ApiExceptionFilterAttribute>();
});

// 3. OpenAPI Specification
builder.Services.AddOpenApi();

// 4. CORS Policy for Angular SPA & Modern Web Clients
builder.Services.AddCors(options =>
{
    options.AddPolicy("WorkflowCorsPolicy", policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// 5. Database Initialization & Seed Master Data on Startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var dbContext = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();

    try
    {
        if (dbContext.Database.IsRelational())
        {
            logger.LogInformation("Applying relational database migrations ({Provider})...", dbContext.Database.ProviderName);
            await dbContext.Database.MigrateAsync();
        }
        else
        {
            logger.LogInformation("Ensuring database schema exists...");
            await dbContext.Database.EnsureCreatedAsync();
        }

        logger.LogInformation("Seeding default workflow master configurations...");
        await WorkflowDbContextSeed.SeedDefaultWorkflowsAsync(dbContext);

        logger.LogInformation("Database ready and verified.");
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Could not initialize primary database provider ({Provider}). Verify connection string or start PostgreSQL container.", dbContext.Database.ProviderName);
    }
}

// 6. Request Pipeline Configuration (OpenAPI & Swagger UI)
app.MapOpenApi();

app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/openapi/v1.json", "Workflow Engine API v1 (Enterprise .NET Core)");
    options.RoutePrefix = "swagger";
    options.DocumentTitle = "Workflow Engine Swagger UI";
});

app.UseCors("WorkflowCorsPolicy");

app.UseRouting();

// Map SignalR Hub for real-time live notifications
app.MapHub<WorkflowNotificationHub>("/hubs/workflow-notification");

// Map REST Controllers
app.MapControllers();

// Health check & API Index endpoint
app.MapGet("/", () => Results.Ok(new
{
    Application = "Enterprise Workflow Engine API (.NET Core 10 LTS)",
    Status = "Healthy",
    Version = "2.1.0",
    ServerTimeUtc = DateTime.UtcNow,
    SwaggerUI = "/swagger",
    OpenApiSchema = "/openapi/v1.json",
    Endpoints = new
    {
        Swagger = "/swagger",
        Start = "/api/v1/workflow/start",
        Get = "/api/v1/workflow/get",
        Next = "/api/v1/workflow/next",
        Approvers = "/api/v1/workflow/approver-candidates",
        Attachment = "/api/v1/workflow/attachment",
        Delegations = "/api/v1/workflow/delegations",
        Dashboard = "/api/v1/dev/dashboard",
        SignalRHub = "/hubs/workflow-notification",
        MasterTipe = "/api/v1/master/workflow/tipe",
        MasterUrutan = "/api/v1/master/workflow/urutan",
        MasterDelegation = "/api/v1/master/workflow/delegation"
    }
}));

app.Run();
