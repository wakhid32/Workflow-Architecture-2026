using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using UserManagement.Application.Common;
using UserManagement.Application.Interfaces;
using UserManagement.Domain.Entities;
using UserManagement.Domain.Events;
using UserManagement.Domain.Exceptions;

namespace UserManagement.Application.Services;

/// <summary>
/// Implementasi layanan manajemen data master karyawan.
/// Menangani CRUD M_STAF, strict 1-to-1 Keycloak linking,
/// invalidasi cache, dan propagasi domain events.
/// </summary>
public sealed class StaffManagementService : IStaffManagementService
{
    private readonly IUserManagementDbContext _dbContext;
    private readonly IKeycloakAdminService _keycloakAdmin;
    private readonly ITokenBlacklistService _tokenBlacklist;
    private readonly ILogger<StaffManagementService> _logger;

    public StaffManagementService(
        IUserManagementDbContext dbContext,
        IKeycloakAdminService keycloakAdmin,
        ITokenBlacklistService tokenBlacklist,
        ILogger<StaffManagementService> logger)
    {
        _dbContext = dbContext;
        _keycloakAdmin = keycloakAdmin;
        _tokenBlacklist = tokenBlacklist;
        _logger = logger;
    }

    public async Task<StaffDetailDto> GetStaffByIdAsync(long staffId, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.IdStaf == staffId, ct)
            ?? throw new StaffNotFoundException(staffId);

        return MapToDetailDto(staff);
    }

    public async Task<PagedResult<StaffListItemDto>> GetAllStaffAsync(StaffQueryRequest query, CancellationToken ct)
    {
        var queryable = _dbContext.MStaf.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var term = query.SearchTerm.ToLower();
            queryable = queryable.Where(s =>
                s.Nip.ToLower().Contains(term) ||
                s.NamaLengkap.ToLower().Contains(term) ||
                s.Email.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.KodeDepartemen))
            queryable = queryable.Where(s => s.KodeDepartemen == query.KodeDepartemen);

        if (!string.IsNullOrWhiteSpace(query.KodeJabatan))
            queryable = queryable.Where(s => s.KodeJabatan == query.KodeJabatan);

        if (query.IsActive.HasValue)
            queryable = queryable.Where(s => s.IsActive == query.IsActive.Value);

        var totalCount = await queryable.CountAsync(ct);
        var items = await queryable
            .OrderBy(s => s.NamaLengkap)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(s => new StaffListItemDto(
                s.IdStaf,
                s.Nip,
                s.NamaLengkap,
                s.Email,
                s.KodeDepartemen,
                s.KodeJabatan,
                s.IsActive,
                s.KeycloakUserId != null))
            .ToListAsync(ct);

        return new PagedResult<StaffListItemDto>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<StaffDetailDto> CreateStaffAsync(CreateStaffRequest request, CancellationToken ct)
    {
        // Validasi uniqueness NIP dan Email
        var existingNip = await _dbContext.MStaf.AnyAsync(s => s.Nip == request.Nip, ct);
        if (existingNip)
            throw new InvalidOperationException($"NIP '{request.Nip}' sudah digunakan oleh staf lain.");

        var existingEmail = await _dbContext.MStaf.AnyAsync(s => s.Email == request.Email, ct);
        if (existingEmail)
            throw new InvalidOperationException($"Email '{PiiMaskingUtility.MaskEmail(request.Email)}' sudah digunakan oleh staf lain.");

        // Validasi atasan langsung jika ada
        if (request.IdAtasanLangsung.HasValue)
        {
            var supervisorExists = await _dbContext.MStaf
                .AnyAsync(s => s.IdStaf == request.IdAtasanLangsung.Value && s.IsActive, ct);
            if (!supervisorExists)
                throw new StaffNotFoundException($"Atasan langsung ID {request.IdAtasanLangsung.Value}");
        }

        var staff = new MStaf
        {
            Nip = request.Nip,
            NamaLengkap = request.NamaLengkap,
            Email = request.Email,
            NomorHp = request.NomorHp,
            KodeDepartemen = request.KodeDepartemen,
            KodeJabatan = request.KodeJabatan,
            IdAtasanLangsung = request.IdAtasanLangsung,
            CreatedBy = "SYSTEM",
            UpdatedBy = "SYSTEM"
        };

        _dbContext.MStaf.Add(staff);
        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Staf baru berhasil dibuat: ID={StaffId}, NIP={Nip}", staff.IdStaf, staff.Nip);

        return MapToDetailDto(staff);
    }

    public async Task<StaffDetailDto> UpdateStaffAsync(long staffId, UpdateStaffRequest request, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf.FirstOrDefaultAsync(s => s.IdStaf == staffId, ct)
            ?? throw new StaffNotFoundException(staffId);

        // Validasi email uniqueness (kecuali milik sendiri)
        var emailConflict = await _dbContext.MStaf
            .AnyAsync(s => s.Email == request.Email && s.IdStaf != staffId, ct);
        if (emailConflict)
            throw new InvalidOperationException($"Email '{PiiMaskingUtility.MaskEmail(request.Email)}' sudah digunakan oleh staf lain.");

        staff.NamaLengkap = request.NamaLengkap;
        staff.Email = request.Email;
        staff.NomorHp = request.NomorHp;
        staff.KodeDepartemen = request.KodeDepartemen;
        staff.KodeJabatan = request.KodeJabatan;
        staff.IdAtasanLangsung = request.IdAtasanLangsung;
        staff.UpdatedDate = DateTime.UtcNow;
        staff.UpdatedBy = "SYSTEM";

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation("Staf berhasil diperbarui: ID={StaffId}", staffId);

        return MapToDetailDto(staff);
    }

    public async Task DeleteStaffAsync(long staffId, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf.FirstOrDefaultAsync(s => s.IdStaf == staffId, ct)
            ?? throw new StaffNotFoundException(staffId);

        // Soft delete
        staff.IsActive = false;
        staff.UpdatedDate = DateTime.UtcNow;
        staff.UpdatedBy = "SYSTEM";

        await _dbContext.SaveChangesAsync(ct);

        // Cascade: disable Keycloak user & logout all sessions
        if (!string.IsNullOrEmpty(staff.KeycloakUserId))
        {
            try
            {
                await _keycloakAdmin.DisableUserAsync(staff.KeycloakUserId, ct);
                await _keycloakAdmin.LogoutAllSessionsAsync(staff.KeycloakUserId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Gagal menonaktifkan akun Keycloak untuk staf ID={StaffId}. " +
                    "Akun akan dinonaktifkan saat sinkronisasi berikutnya.", staffId);
            }
        }

        // Emit domain event for cascade handlers
        var domainEvent = StaffDeactivatedEvent.Create(
            staff.IdStaf, staff.Nip, staff.KeycloakUserId, "Soft deleted by admin");

        _logger.LogInformation(
            "Staf dinonaktifkan (soft delete): ID={StaffId}, NIP={Nip}, Event={Event}",
            staffId, staff.Nip, domainEvent);
    }

    public async Task LinkKeycloakUserAsync(long staffId, string keycloakUserId, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf.FirstOrDefaultAsync(s => s.IdStaf == staffId, ct)
            ?? throw new StaffNotFoundException(staffId);

        if (!staff.IsActive)
            throw new InvalidOperationException($"Staf ID {staffId} tidak aktif. Tidak dapat menautkan akun.");

        // Strict 1-to-1 enforcement (application level, complementing DB filtered unique index)
        var existingLink = await _dbContext.MStaf
            .FirstOrDefaultAsync(s => s.KeycloakUserId == keycloakUserId && s.IsActive && s.IdStaf != staffId, ct);

        if (existingLink is not null)
            throw new DuplicateLinkingException(keycloakUserId, existingLink.Nip);

        staff.KeycloakUserId = keycloakUserId;
        staff.UpdatedDate = DateTime.UtcNow;
        staff.UpdatedBy = "SYSTEM";

        await _dbContext.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Akun Keycloak ditautkan: StaffId={StaffId}, KeycloakUserId={KeycloakUserId}",
            staffId, keycloakUserId);
    }

    public async Task UnlinkKeycloakUserAsync(long staffId, CancellationToken ct)
    {
        var staff = await _dbContext.MStaf.FirstOrDefaultAsync(s => s.IdStaf == staffId, ct)
            ?? throw new StaffNotFoundException(staffId);

        var previousKeycloakId = staff.KeycloakUserId;
        staff.KeycloakUserId = null;
        staff.UpdatedDate = DateTime.UtcNow;
        staff.UpdatedBy = "SYSTEM";

        await _dbContext.SaveChangesAsync(ct);

        // Logout all sessions for the unlinked user
        if (!string.IsNullOrEmpty(previousKeycloakId))
        {
            try
            {
                await _keycloakAdmin.LogoutAllSessionsAsync(previousKeycloakId, ct);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Gagal melakukan logout sesi Keycloak saat unlink staf ID={StaffId}.", staffId);
            }
        }

        _logger.LogInformation(
            "Akun Keycloak dilepaskan: StaffId={StaffId}, PreviousKeycloakId={PreviousKeycloakId}",
            staffId, previousKeycloakId);
    }

    public async Task<StaffDetailDto> ProvisionStaffWithAccountAsync(ProvisionStaffRequest request, CancellationToken ct)
    {
        // Step 1: Buat data staf terlebih dahulu
        var staffResult = await CreateStaffAsync(
            new CreateStaffRequest(
                request.Nip,
                request.NamaLengkap,
                request.Email,
                request.NomorHp,
                request.KodeDepartemen,
                request.KodeJabatan,
                request.IdAtasanLangsung),
            ct);

        try
        {
            // Step 2: Buat akun Keycloak
            var nameParts = request.NamaLengkap.Split(' ', 2);
            var keycloakUserId = await _keycloakAdmin.CreateUserAsync(
                new CreateKeycloakUserRequest(
                    request.Username,
                    request.Email,
                    request.Password,
                    nameParts[0],
                    nameParts.Length > 1 ? nameParts[1] : string.Empty),
                ct);

            // Step 3: Assign role
            await _keycloakAdmin.AssignRealmRoleAsync(keycloakUserId, request.RoleName, ct);

            // Step 4: Link Keycloak user ke staf
            await LinkKeycloakUserAsync(staffResult.IdStaf, keycloakUserId, ct);

            _logger.LogInformation(
                "One-step provisioning berhasil: StaffId={StaffId}, KeycloakUserId={KeycloakUserId}",
                staffResult.IdStaf, keycloakUserId);

            return staffResult with { KeycloakUserId = keycloakUserId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Gagal provisioning akun Keycloak untuk staf ID={StaffId}. " +
                "Data staf tetap tersimpan, silakan link manual.", staffResult.IdStaf);
            throw;
        }
    }

    private static StaffDetailDto MapToDetailDto(MStaf staff) => new(
        staff.IdStaf,
        staff.Nip,
        staff.NamaLengkap,
        staff.Email,
        staff.NomorHp,
        staff.KodeDepartemen,
        staff.KodeJabatan,
        staff.IdAtasanLangsung,
        staff.KeycloakUserId,
        staff.IsActive,
        staff.CreatedDate,
        staff.UpdatedDate);
}
