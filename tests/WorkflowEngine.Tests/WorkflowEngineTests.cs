using Microsoft.EntityFrameworkCore;
using Moq;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Persistence.Seed;
using WorkflowEngine.Infrastructure.Services;
using Xunit;

namespace WorkflowEngine.Tests;

public class WorkflowEngineTests
{
    private static async Task<(WorkflowDbContext context, WorkflowEngineService service, Mock<ICurrentUserService> userMock)> CreateTestContextAsync(long currentUserId = 1)
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: $"WorkflowDb_{Guid.NewGuid():N}")
            .Options;

        var context = new WorkflowDbContext(options);
        await WorkflowDbContextSeed.SeedDefaultWorkflowsAsync(context);

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(currentUserId);
        userMock.Setup(u => u.UserName).Returns($"User #{currentUserId}");
        userMock.Setup(u => u.ClientIpAddress).Returns("192.168.1.100");
        userMock.Setup(u => u.UserAgent).Returns("Mozilla/5.0 TestBrowser");
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var approverStrategy = new ApproverResolutionStrategy(context);

        var fileStorageMock = new Mock<IFileStorageService>();
        fileStorageMock.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("test-path/file.pdf", 1024L));

        var service = new WorkflowEngineService(context, approverStrategy, userMock.Object, fileStorageMock.Object);

        return (context, service, userMock);
    }

    [Fact]
    public async Task StartWorkflow_ShouldReturnInitialButtonsAndCandidates()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 1);

        // Act
        var result = await service.GetStartWorkflowAsync(new StartWorkflowRequest(201, 501));

        // Assert
        Assert.NotNull(result);
        Assert.Equal(201, result.TipeDisposisi);
        Assert.Equal("CAPEX.DRAFT", result.StatusDokumenAwal);
        Assert.NotEmpty(result.ActionButtons);
        Assert.Contains(result.ActionButtons, b => b.ActionType == "SUBMIT");
        Assert.NotEmpty(result.ApproverCandidates);
    }

    [Fact]
    public async Task StartWorkflow_InvalidType_ShouldThrowNotFoundException()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 1);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.GetStartWorkflowAsync(new StartWorkflowRequest(9999)));
    }

    [Fact]
    public async Task ExecuteNextDisposisi_InitialDraft_ShouldCreateTaskAndOutboxEvent()
    {
        // Arrange
        var (context, service, _) = await CreateTestContextAsync(currentUserId: 1);

        var request = new NextDisposisiRequest(
            IdTask: 0,
            HashId: null,
            IdUrutanDisposisi: 1, // Submit ke Atasan Langsung
            IdDokumen: "DOC-2026-001",
            NoDokumen: "PR/2026/001",
            IdForm: 501,
            SelectedApproverId: 2, // Siti Rahmawati
            CatatanDisposisi: "Pengajuan Capex baru",
            DocumentAmount: 25000000
        );

        // Act
        var result = await service.ExecuteNextDisposisiAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.IdTask > 0);
        Assert.Equal("CAPEX.WAITING_SUPERVISOR", result.StatusDokumenBaru);
        Assert.False(result.IsCompleted);

        // Verify task in DB
        var savedTask = await context.WtTasklist.FirstOrDefaultAsync(t => t.IdTask == result.IdTask);
        Assert.NotNull(savedTask);
        Assert.Equal(2, savedTask.IdUserTujuan);
        Assert.Equal(1, savedTask.IdUserAsal);
        Assert.Equal(24, savedTask.SlaHours);
        Assert.NotNull(savedTask.DueDate);

        // Verify Outbox Event created
        var outboxMessage = await context.WtWorkflowOutbox.FirstOrDefaultAsync(o => o.IdTask == result.IdTask);
        Assert.NotNull(outboxMessage);
        Assert.Equal("TASK_ASSIGNED", outboxMessage.EventType);
        Assert.Equal(OutboxStatus.Pending, outboxMessage.Status);
    }

    [Fact]
    public async Task ExecuteNextDisposisi_AmountLimitViolation_ShouldThrowWorkflowException()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 2);

        // Step 2 in PR (Modul 101) has AmtLimitMax = 50,000,000
        var request = new NextDisposisiRequest(
            IdTask: 0,
            HashId: null,
            IdUrutanDisposisi: 7, // PR Step 2 (Approve) max 50jt
            IdDokumen: "PR-2026-002",
            NoDokumen: "PR/2026/002",
            IdForm: 101,
            SelectedApproverId: 2,
            CatatanDisposisi: "Approval nominal besar",
            DocumentAmount: 100000000 // 100 Juta > 50 Juta limit
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ExecuteNextDisposisiAsync(request));

        Assert.Contains("di luar batas kewenangan nominal", ex.Message);
    }

    [Fact]
    public async Task ExecuteNextDisposisi_MandatoryAttachmentMissing_ShouldThrowWorkflowException()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 5);

        // Step 5 in Capex (IdUrutan 5) is Reject which has IsRequireAttachment = true
        var request = new NextDisposisiRequest(
            IdTask: 0,
            HashId: null,
            IdUrutanDisposisi: 5, // Capex Reject - Requires Attachment
            IdDokumen: "CPX-003",
            NoDokumen: "CPX/003",
            IdForm: 501,
            CatatanDisposisi: "Tolak tanpa bukti lampiran telaah",
            DocumentAmount: 10000000,
            UploadedAttachmentIds: null // No attachments!
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<WorkflowException>(() =>
            service.ExecuteNextDisposisiAsync(request));

        Assert.Contains("mewajibkan pengunggahan berkas lampiran pendukung", ex.Message);
    }

    [Fact]
    public async Task ExecuteNextDisposisi_ActingOfficer_ShouldHaveDelegatedAuthority()
    {
        // Arrange: User 2 (Siti) delegated to User 6 (Rina) in Seed data!
        var (context, service, userMock) = await CreateTestContextAsync(currentUserId: 1);

        // 1. User 1 submits to User 2
        var submitResult = await service.ExecuteNextDisposisiAsync(new NextDisposisiRequest(
            IdTask: 0,
            HashId: null,
            IdUrutanDisposisi: 1,
            IdDokumen: "CPX-DEL-01",
            NoDokumen: "CPX/DEL/01",
            IdForm: 501,
            SelectedApproverId: 2, // Assigned to Siti (User 2)
            CatatanDisposisi: "Submit document",
            DocumentAmount: 10000000
        ));

        // 2. Switch active user to User 6 (Rina - Plt / Delegate)
        userMock.Setup(u => u.UserId).Returns(6);
        userMock.Setup(u => u.UserName).Returns("Rina Marlina (Plt)");

        // 3. User 6 fetches task detail
        var detail = await service.GetWorkflowDetailAsync(new GetWorkflowTaskRequest(IdTask: submitResult.IdTask));
        Assert.True(detail.IsActivePic, "Plt/Pjs must have active PIC authority due to delegation.");
        Assert.NotEmpty(detail.PermittedButtons);

        // 4. User 6 approves on behalf of User 2
        var approveResult = await service.ExecuteNextDisposisiAsync(new NextDisposisiRequest(
            IdTask: submitResult.IdTask,
            HashId: null,
            IdUrutanDisposisi: 2, // Step 2: Approve & forward to Direksi
            IdDokumen: "CPX-DEL-01",
            NoDokumen: "CPX/DEL/01",
            IdForm: 501,
            SelectedApproverId: 5,
            CatatanDisposisi: "Disetujui oleh Plt (Siti sedang dinas luar)",
            DocumentAmount: 10000000,
            RowVersion: detail.RowVersion
        ));

        Assert.True(approveResult.Success);
        Assert.Equal("CAPEX.WAITING_FINANCE_DIR", approveResult.StatusDokumenBaru);

        // Verify audit trail recorded executor as User 6
        var hist = await context.WtTasklistHist
            .OrderByDescending(h => h.IdTaskHist)
            .FirstOrDefaultAsync();

        Assert.NotNull(hist);
        Assert.Equal(6, hist.IdUserEksekutor);
    }

    [Fact]
    public async Task ExecuteNextDisposisi_UnauthorizedUser_ShouldThrowForbiddenException()
    {
        // Arrange
        var (context, service, userMock) = await CreateTestContextAsync(currentUserId: 1);

        // 1. User 1 submits to User 2
        var submitResult = await service.ExecuteNextDisposisiAsync(new NextDisposisiRequest(
            IdTask: 0,
            HashId: null,
            IdUrutanDisposisi: 1,
            IdDokumen: "CPX-UNAUTH-01",
            NoDokumen: "CPX/UNAUTH/01",
            IdForm: 501,
            SelectedApproverId: 2,
            CatatanDisposisi: "Submit",
            DocumentAmount: 10000000
        ));

        // 2. Switch active user to User 99 (Completely unrelated user)
        userMock.Setup(u => u.UserId).Returns(99);
        userMock.Setup(u => u.UserName).Returns("Random User");

        // 3. User 99 tries to approve
        var ex = await Assert.ThrowsAsync<ForbiddenException>(() =>
            service.ExecuteNextDisposisiAsync(new NextDisposisiRequest(
                IdTask: submitResult.IdTask,
                HashId: null,
                IdUrutanDisposisi: 2,
                IdDokumen: "CPX-UNAUTH-01",
                NoDokumen: "CPX/UNAUTH/01",
                IdForm: 501,
                SelectedApproverId: 5,
                CatatanDisposisi: "Sneaky approve attempt",
                DocumentAmount: 10000000
            )));

        Assert.Contains("kewenangan approval", ex.Message);
    }
}
