namespace UserManagement.Domain.Enums;

/// <summary>
/// Level posisi/jabatan karyawan dalam hierarki organisasi.
/// Digunakan untuk resolusi approver dan eskalasi disposisi.
/// </summary>
public enum StaffPositionLevel
{
    /// <summary>Karyawan biasa / pelaksana.</summary>
    Staff = 0,

    /// <summary>Kepala Seksi / Supervisor.</summary>
    SectionHead = 1,

    /// <summary>Manajer / Kepala Departemen.</summary>
    Manager = 2,

    /// <summary>Direktur / Pimpinan Puncak.</summary>
    Director = 3
}
