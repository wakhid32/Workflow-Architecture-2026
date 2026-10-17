namespace UserManagement.Domain.Entities;

/// <summary>
/// Entitas audit forensik keamanan (SEC_AUDIT_LOG) pada database security_identity_db.
/// Merekam setiap aksi autentikasi, manajemen staf, dan event keamanan
/// dengan korelasi terdistribusi (W3C Trace Context).
/// </summary>
public sealed class SecAuditLog
{
    public long IdLog { get; set; }

    /// <summary>
    /// W3C Trace Context / Correlation ID untuk pelacakan end-to-end.
    /// </summary>
    public string CorrelationId { get; set; } = string.Empty;

    /// <summary>
    /// Nama aksi yang direkam (LOGIN, LOGOUT, REGISTER, CHANGE_PASSWORD, STAFF_CREATE, dll.).
    /// </summary>
    public string ActionName { get; set; } = string.Empty;

    /// <summary>
    /// Keycloak User ID (Sub claim) dari pelaku aksi.
    /// </summary>
    public string ActorUserId { get; set; } = string.Empty;

    /// <summary>
    /// ID staf internal (M_STAF.ID_STAF) dari pelaku aksi, jika tersedia.
    /// </summary>
    public long? ActorStaffId { get; set; }

    /// <summary>
    /// Nama resource yang menjadi target aksi (USER, STAFF, SESSION, dll.).
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Identifier resource target (misalnya Staff ID, User ID).
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Hasil aksi: SUCCESS, FAILED, BLOCKED.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public string ClientIp { get; set; } = string.Empty;
    public string? UserAgent { get; set; }

    /// <summary>
    /// Snapshot JSON dari payload request (sudah disanitasi PII).
    /// </summary>
    public string? PayloadSnapshot { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
