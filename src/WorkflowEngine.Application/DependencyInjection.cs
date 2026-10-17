using Microsoft.Extensions.DependencyInjection;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows;

namespace WorkflowEngine.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IWorkflowEngineService, WorkflowEngineService>();
        services.AddScoped<IWorkflowMasterService, WorkflowMasterService>();
        return services;
    }
}
