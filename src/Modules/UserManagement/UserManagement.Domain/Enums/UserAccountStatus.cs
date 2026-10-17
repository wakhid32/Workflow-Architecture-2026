namespace UserManagement.Domain.Enums;

/// <summary>
/// Status siklus hidup akun staf dalam sistem.
/// </summary>
public enum UserAccountStatus
{
    /// <summary>Akun aktif dan dapat digunakan.</summary>
    Active = 0,

    /// <summary>Akun ditangguhkan sementara oleh admin (tidak dapat login).</summary>
    Suspended = 1,

    /// <summary>Karyawan telah resign/pensiun (akun dinonaktifkan permanen).</summary>
    Resigned = 2
}
