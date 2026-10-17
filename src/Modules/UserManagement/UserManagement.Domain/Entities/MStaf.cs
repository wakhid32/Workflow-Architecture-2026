namespace UserManagement.Domain.Entities;

/// <summary>
/// Entitas master data karyawan (M_STAF) pada database security_identity_db.
/// Menyimpan profil lengkap staf, relasi hierarki atasan langsung,
/// dan tautan 1-to-1 ke akun Keycloak.
/// </summary>
public sealed class MStaf
{
    public long IdStaf { get; set; }
    public string Nip { get; set; } = string.Empty;
    public string NamaLengkap { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? NomorHp { get; set; }
    public string KodeDepartemen { get; set; } = string.Empty;
    public string KodeJabatan { get; set; } = string.Empty;

    /// <summary>
    /// FK self-referencing ke atasan langsung.
    /// NULL jika staf adalah puncak hierarki (misalnya Direktur Utama).
    /// </summary>
    public long? IdAtasanLangsung { get; set; }

    /// <summary>
    /// UUID akun Keycloak yang tertaut.
    /// Dijaga oleh Filtered Unique Index: hanya 1 staf aktif per user Keycloak.
    /// </summary>
    public string? KeycloakUserId { get; set; }

    public bool IsActive { get; set; } = true;

    // Audit Columns
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime UpdatedDate { get; set; } = DateTime.UtcNow;
    public string UpdatedBy { get; set; } = string.Empty;

    // Navigation Properties
    public MStaf? AtasanLangsung { get; set; }
    public ICollection<MStaf> Bawahan { get; set; } = new List<MStaf>();
}
