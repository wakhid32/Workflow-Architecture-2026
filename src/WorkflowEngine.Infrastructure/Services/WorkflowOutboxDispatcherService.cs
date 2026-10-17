using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Domain.Interfaces;
using WorkflowEngine.Infrastructure.Hubs;

namespace WorkflowEngine.Infrastructure.Services;

/// <summary>
/// Background Service Outbox Dispatcher.
/// Menjamin pesan notifikasi/event terkirim secara asinkron (Eventual Consistency)
/// dan dipush via SignalR hub tanpa memperlambat transaksi database utama.
/// </summary>
public sealed class WorkflowOutboxDispatcherService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<WorkflowOutboxDispatcherService> _logger;

    public WorkflowOutboxDispatcherService(
        IServiceProvider serviceProvider,
        ILogger<WorkflowOutboxDispatcherService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Workflow Transactional Outbox Dispatcher started.");

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<IWorkflowDbContext>();
                var hubContext = scope.ServiceProvider.GetRequiredService<IHubContext<WorkflowNotificationHub>>();

                var pendingMessages = await context.WtWorkflowOutbox
                    .Where(o => o.Status == OutboxStatus.Pending && o.RetryCount < 5)
                    .OrderBy(o => o.CreatedDate)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (pendingMessages.Count > 0)
                {
                    foreach (var message in pendingMessages)
                    {
                        try
                        {
                            long targetUserId = 0;
                            using (var doc = JsonDocument.Parse(message.RecipientPayloadJson))
                            {
                                if (doc.RootElement.TryGetProperty("TargetUserId", out var targetProp))
                                {
                                    targetUserId = targetProp.GetInt64();
                                }
                                else if (doc.RootElement.TryGetProperty("UserId", out var userProp))
                                {
                                    targetUserId = userProp.GetInt64();
                                }
                            }

                            if (targetUserId > 0)
                            {
                                await hubContext.Clients.Group($"User_{targetUserId}")
                                    .SendAsync("ReceiveWorkflowNotification", new
                                    {
                                        OutboxId = message.IdOutbox,
                                        TaskId = message.IdTask,
                                        message.EventType,
                                        Payload = message.MessagePayloadJson,
                                        Timestamp = DateTime.UtcNow
                                    }, stoppingToken);
                            }

                            message.Status = OutboxStatus.Sent;
                            message.ProcessedDate = DateTime.UtcNow;
                            message.LastError = null;
                        }
                        catch (Exception ex)
                        {
                            message.RetryCount++;
                            message.LastError = ex.Message;
                            if (message.RetryCount >= 5)
                            {
                                message.Status = OutboxStatus.Failed;
                            }
                            _logger.LogWarning(ex, "Failed to dispatch outbox message {IdOutbox}, retry {RetryCount}", message.IdOutbox, message.RetryCount);
                        }
                    }

                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error processing outbox dispatch cycle.");
            }

            // Polling interval
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
