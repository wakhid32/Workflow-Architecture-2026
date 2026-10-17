using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;
using UserManagement.Application.Services;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Exceptions;
using UserManagement.Infrastructure.Caching;
using UserManagement.Infrastructure.Persistence;
using WorkflowEngine.Infrastructure.Services;
using Xunit;

namespace WorkflowEngine.Tests;

/// <summary>
/// Pengujian otomatis keamanan tingkat enterprise (SECURITY.md Section 13):
/// 1. Strict 1-to-1 Keycloak user to staff linking invariant
/// 2. PII Data Masking compliance (UU PDP / GDPR / ISO 27001)
/// 3. Zero-DB-Hop Enriched Claims parsing
/// 4. Token Blacklisting and Instant Revocation
/// 5. Standard Enterprise BaseResponse
/// </summary>
public sealed class UserManagementSecurityTests
{
    private static UserManagementDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<UserManagementDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new UserManagementDbContext(options);
    }

    [Fact]
    public async Task LinkKeycloakUserAsync_ShouldThrowException_WhenKeycloakUserAlreadyLinkedToAnotherActiveStaff()
    {
        // Arrange: Strict 1-to-1 invariant
        using var context = CreateInMemoryDbContext();
        var keycloakAdminMock = new Mock<IKeycloakAdminService>();
        var blacklistMock = new Mock<ITokenBlacklistService>();
        var loggerMock = new Mock<ILogger<StaffManagementService>>();

        var service = new StaffManagementService(context, keycloakAdminMock.Object, blacklistMock.Object, loggerMock.Object);

        var staff1 = new MStaf
        {
            IdStaf = 1,
            Nip = "STAF-001",
            NamaLengkap = "Budi Santoso",
            Email = "budi@company.co.id",
            KodeDepartemen = "IT",
            KodeJabatan = "ENG",
            KeycloakUserId = "keycloak-uuid-123",
            IsActive = true,
            CreatedBy = "System",
            UpdatedBy = "System"
        };

        var staff2 = new MStaf
        {
            IdStaf = 2,
            Nip = "STAF-002",
            NamaLengkap = "Agus Prasetyo",
            Email = "agus@company.co.id",
            KodeDepartemen = "HR",
            KodeJabatan = "HR_STAFF",
            KeycloakUserId = null,
            IsActive = true,
            CreatedBy = "System",
            UpdatedBy = "System"
        };

        context.MStaf.AddRange(staff1, staff2);
        await context.SaveChangesAsync();

        // Act & Assert: Attempting to link staff2 with the already linked "keycloak-uuid-123" must fail
        await Assert.ThrowsAsync<DuplicateLinkingException>(async () =>
        {
            await service.LinkKeycloakUserAsync(2, "keycloak-uuid-123", CancellationToken.None);
        });
    }

    [Fact]
    public async Task LinkKeycloakUserAsync_ShouldSucceed_WhenKeycloakUserIsNotLinked()
    {
        // Arrange
        using var context = CreateInMemoryDbContext();
        var keycloakAdminMock = new Mock<IKeycloakAdminService>();
        var blacklistMock = new Mock<ITokenBlacklistService>();
        var loggerMock = new Mock<ILogger<StaffManagementService>>();

        var service = new StaffManagementService(context, keycloakAdminMock.Object, blacklistMock.Object, loggerMock.Object);

        var staff = new MStaf
        {
            IdStaf = 1,
            Nip = "STAF-001",
            NamaLengkap = "Budi Santoso",
            Email = "budi@company.co.id",
            KodeDepartemen = "IT",
            KodeJabatan = "ENG",
            KeycloakUserId = null,
            IsActive = true,
            CreatedBy = "System",
            UpdatedBy = "System"
        };

        context.MStaf.Add(staff);
        await context.SaveChangesAsync();

        // Act
        await service.LinkKeycloakUserAsync(1, "new-uuid-456", CancellationToken.None);

        // Assert
        var updated = await context.MStaf.FindAsync(1L);
        Assert.NotNull(updated);
        Assert.Equal("new-uuid-456", updated.KeycloakUserId);
    }

    [Fact]
    public void PiiMaskingUtility_ShouldMaskSensitiveDataProperly()
    {
        // Phone number masking
        var maskedPhone = PiiMaskingUtility.MaskPhoneNumber("081234567890");
        Assert.StartsWith("0812", maskedPhone);
        Assert.EndsWith("7890", maskedPhone);
        Assert.Contains("****", maskedPhone);

        // Email masking
        var maskedEmail = PiiMaskingUtility.MaskEmail("budi.santoso@company.co.id");
        Assert.StartsWith("b***", maskedEmail);
        Assert.EndsWith("@company.co.id", maskedEmail);

        // Payload sanitization
        var payload = "{\"username\":\"budi\",\"password\":\"supersecret123\",\"token\":\"jwt-secret-token\"}";
        var sanitized = PiiMaskingUtility.SanitizePayload(payload);
        Assert.Contains("\"password\":\"[REDACTED]\"", sanitized);
        Assert.Contains("\"token\":\"[REDACTED]\"", sanitized);
        Assert.DoesNotContain("supersecret123", sanitized);
    }

    [Fact]
    public void CurrentUserService_ShouldResolveEnrichedClaimsWithoutDatabaseQuery()
    {
        // Arrange: Claims issued after Keycloak login enriched with staff metadata
        var claims = new List<Claim>
        {
            new("sub", "a8c90962-d27a-4ec6-8c46-991c01e6191b"),
            new("preferred_username", "budi.santoso"),
            new("email", "budi.santoso@company.co.id"),
            new("staff_id", "42"),
            new("staff_nip", "STAF-2026-042"),
            new("department_code", "IT_INFRA"),
            new("position_code", "SECTION_HEAD"),
            new(ClaimTypes.Role, "ROLE_USER"),
            new(ClaimTypes.Role, "ROLE_SECTION_HEAD")
        };

        var identity = new ClaimsIdentity(claims, "Bearer");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext
        {
            User = principal
        };
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("192.168.1.100");

        var httpAccessorMock = new Mock<IHttpContextAccessor>();
        httpAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var currentUserService = new CurrentUserService(httpAccessorMock.Object);

        // Act & Assert: Zero-DB-Hop claim resolution
        Assert.Equal(42L, currentUserService.UserId);
        Assert.Equal(42L, currentUserService.StaffId);
        Assert.Equal("budi.santoso", currentUserService.UserName);
        Assert.Equal("STAF-2026-042", currentUserService.StaffNip);
        Assert.Equal("IT_INFRA", currentUserService.DepartmentCode);
        Assert.Equal("SECTION_HEAD", currentUserService.PositionCode);
        Assert.Equal("a8c90962-d27a-4ec6-8c46-991c01e6191b", currentUserService.KeycloakUserId);
        Assert.Equal("budi.santoso@company.co.id", currentUserService.Email);
        Assert.Contains("ROLE_USER", currentUserService.Roles);
        Assert.Contains("ROLE_SECTION_HEAD", currentUserService.Roles);
    }

    [Fact]
    public async Task RedisTokenBlacklistService_ShouldBlacklistAndDetectRevokedToken()
    {
        // Arrange: Mock IDistributedCache with in-memory dict
        var cacheStorage = new Dictionary<string, byte[]>();
        var cacheMock = new Mock<IDistributedCache>();

        cacheMock.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string key, CancellationToken _) =>
                cacheStorage.TryGetValue(key, out var val) ? val : null);

        cacheMock.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
            .Returns((string key, byte[] val, DistributedCacheEntryOptions _, CancellationToken _) =>
            {
                cacheStorage[key] = val;
                return Task.CompletedTask;
            });

        var loggerMock = new Mock<ILogger<RedisTokenBlacklistService>>();
        var blacklistService = new RedisTokenBlacklistService(cacheMock.Object, loggerMock.Object);

        var jti = "test-jti-" + Guid.NewGuid();

        // Initially not blacklisted
        var isInitiallyBlacklisted = await blacklistService.IsTokenBlacklistedAsync(jti, CancellationToken.None);
        Assert.False(isInitiallyBlacklisted);

        // Act: Blacklist token for 1 hour
        await blacklistService.BlacklistTokenAsync(jti, TimeSpan.FromHours(1), CancellationToken.None);

        // Assert: Verified blacklisted
        var isBlacklisted = await blacklistService.IsTokenBlacklistedAsync(jti, CancellationToken.None);
        Assert.True(isBlacklisted);
    }

    [Fact]
    public void BaseResponse_ShouldSerializeStandardStructure()
    {
        // Act: Standard Success response
        var response = BaseResponse<string>.Ok("TestData", "Operasi berhasil.");

        // Assert
        Assert.True(response.Success);
        Assert.Equal("Operasi berhasil.", response.Message);
        Assert.Equal("TestData", response.Data);

        // Fail response
        var failResponse = BaseResponse<string>.Fail("Terjadi kesalahan.");
        Assert.False(failResponse.Success);
        Assert.Equal("Terjadi kesalahan.", failResponse.Message);
        Assert.Null(failResponse.Data);
    }
}
