using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Domain.Interfaces;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Background Service untuk pemantauan kepatuhan SLA persetujuan dokumen.
/// Mendeteksi dokumen yang melewati tenggat (Due Date), menandai flag IsOverdue,
/// dan menerbitkan event eskalasi ke antrean outbox.
/// </summary>
public sealed class WorkflowSlaMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowSlaMonitoringService> _logger;

    public WorkflowSlaMonitoringService(
        IServiceProvider serviceProvider,
        ILogger<WorkflowSlaMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow SLA Monitoring Engine started successfully.");

        // Memberikan jeda awal agar aplikasi selesai bootstrap
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IWorkflowDbContext>();
                var now = DateTime.UtcNow;

                var overdueTasks = await context.WtTasklist
                    .Where(t => t.TglDispAkhir == null
                             && !t.IsOverdue
                             && t.DueDate != null
                             && t.DueDate < now)
                    .ToListAsync(stoppingToken);

                if (overdueTasks.Count > 0)
                {
                    foreach (var task in overdueTasks)
                    {
                        task.IsOverdue = true;

                        var overdueHours = (now - task.DueDate!.Value).TotalHours;

                        context.WtWorkflowOutbox.Add(new WtWorkflowOutbox
                        {
                            IdTask = task.IdTask,
                            EventType = "SLA_OVERDUE_ESCALATION",
                            RecipientPayloadJson = $"{{\"UserId\": {task.IdUserTujuan}}}",
                            MessagePayloadJson = $"{{\"DocumentNo\": \"{task.NoDokumen}\", \"Status\": \"{task.IdStatusDok}\", \"OverdueHours\": {overdueHours:F1}}}",
                            Status = OutboxStatus.Pending,
                            CreatedDate = now
                        });
                    }

                    await context.SaveChangesAsync(stoppingToken);
                    _logger.LogWarning("SLA Engine: Terdeteksi {Count} dokumen overdue dan dijadwalkan untuk eskalasi.", overdueTasks.Count);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error occurred during Workflow SLA evaluation cycle.");
            }

            // Interval pengecekan: setiap 1 menit untuk responsiveness
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
