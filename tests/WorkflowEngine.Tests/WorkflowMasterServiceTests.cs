using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using WorkflowEngine.Application.Common.Exceptions;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Application.Workflows;
using WorkflowEngine.Application.Workflows.DTOs;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Persistence.Seed;
using Xunit;

namespace WorkflowEngine.Tests;

public class WorkflowMasterServiceTests
{
    private static async Task<(WorkflowDbContext context, WorkflowMasterService service, Mock<ICurrentUserService> userMock)> CreateTestContextAsync(long currentUserId = 1)
    {
        var options = new DbContextOptionsBuilder<WorkflowDbContext>()
            .UseInMemoryDatabase(databaseName: $"WorkflowMasterDb_{Guid.NewGuid():N}")
            .Options;

        var context = new WorkflowDbContext(options);
        await WorkflowDbContextSeed.SeedDefaultWorkflowsAsync(context);

        var userMock = new Mock<ICurrentUserService>();
        userMock.Setup(u => u.UserId).Returns(currentUserId);
        userMock.Setup(u => u.UserName).Returns($"Admin #{currentUserId}");
        userMock.Setup(u => u.IsAuthenticated).Returns(true);

        var loggerMock = new Mock<ILogger<WorkflowMasterService>>();

        var service = new WorkflowMasterService(context, userMock.Object, loggerMock.Object);

        return (context, service, userMock);
    }

    [Fact]
    public async Task CreateTipeDisposisi_ShouldAddSuccessfully()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync();
        var request = new CreateTipeDisposisiRequest(
            IdTipeDisposisi: 401,
            NamaDisposisi: "Pengajuan Perjalanan Dinas (SPPD)",
            Deskripsi: "Alur persetujuan surat perintah perjalanan dinas",
            KodeModul: "HR_SPPD",
            IsActive: true,
            IsReopenClosed: false
        );

        // Act
        var result = await service.CreateTipeDisposisiAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(401, result.IdTipeDisposisi);
        Assert.Equal("Pengajuan Perjalanan Dinas (SPPD)", result.NamaDisposisi);
        Assert.Equal("HR_SPPD", result.KodeModul);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateTipeDisposisi_ShouldModifyFields()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync();
        var updateRequest = new UpdateTipeDisposisiRequest(
            NamaDisposisi: "Pengajuan Capex & Aset Diperbarui",
            Deskripsi: "Deskripsi diperbarui",
            KodeModul: "ASSET_MGMT_V2",
            IsActive: true,
            IsReopenClosed: true
        );

        // Act
        var result = await service.UpdateTipeDisposisiAsync(201, updateRequest);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Pengajuan Capex & Aset Diperbarui", result.NamaDisposisi);
        Assert.Equal("ASSET_MGMT_V2", result.KodeModul);
        Assert.True(result.IsReopenClosed);
    }

    [Fact]
    public async Task DeleteTipeDisposisi_SoftDelete_ShouldSetIsActiveFalse()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync();

        // Act
        var success = await service.DeleteTipeDisposisiAsync(301, hardDelete: false);
        var entity = await service.GetTipeDisposisiByIdAsync(301);

        // Assert
        Assert.True(success);
        Assert.False(entity.IsActive);
    }

    [Fact]
    public async Task CreateUrutanDisposisi_ShouldAddTransitionStep()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync();
        var request = new CreateUrutanDisposisiRequest(
            IdTipeDisposisi: 201,
            IdRoleAsal: 1,
            IdStatusDokAsal: "CAPEX.REVISED",
            IdRole: 2,
            IdStatusDok: "CAPEX.WAITING_SUPERVISOR",
            IdForm: 501,
            Nama: "Kirim Ulang Hasil Revisi",
            ClassButton: "btn-primary",
            ClassIcon: "fa fa-paper-plane",
            ActionType: WorkflowActionType.Submit,
            ApprovalMode: ApprovalMode.Single,
            NoUrutan: 6,
            IsActive: true
        );

        // Act
        var result = await service.CreateUrutanDisposisiAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IdUrutanDisp > 0);
        Assert.Equal("Kirim Ulang Hasil Revisi", result.Nama);
        Assert.Equal("CAPEX.REVISED", result.IdStatusDokAsal);
        Assert.Equal("CAPEX.WAITING_SUPERVISOR", result.IdStatusDok);
    }

    [Fact]
    public async Task CreateDelegation_ShouldRegisterPltSuccessfully()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 10);
        var request = new CreateDelegationRequest(
            IdUserPenerima: 20,
            IdTipeDisposisi: 201,
            TanggalMulai: DateTime.UtcNow.Date,
            TanggalSelesai: DateTime.UtcNow.Date.AddDays(7),
            AlasanDelegasi: "Cuti Ibadah Umroh"
        );

        // Act
        var result = await service.CreateDelegationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10, result.IdUserPemberi);
        Assert.Equal(20, result.IdUserPenerima);
        Assert.Equal(201, result.IdTipeDisposisi);
        Assert.Equal("Cuti Ibadah Umroh", result.AlasanDelegasi);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateDelegation_WithSameUser_ShouldThrowWorkflowException()
    {
        // Arrange
        var (_, service, _) = await CreateTestContextAsync(currentUserId: 10);
        var request = new CreateDelegationRequest(
            IdUserPenerima: 10, // Penerima sama dengan pemberi
            IdTipeDisposisi: null,
            TanggalMulai: DateTime.UtcNow,
            TanggalSelesai: DateTime.UtcNow.AddDays(1),
            AlasanDelegasi: "Tes delegasi diri sendiri"
        );

        // Act & Assert
        await Assert.ThrowsAsync<WorkflowException>(() => service.CreateDelegationAsync(request));
    }
}
