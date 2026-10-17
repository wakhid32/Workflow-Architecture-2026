namespace UserManagement.Domain.Exceptions;

/// <summary>
/// Exception dilemparkan ketika terjadi pelanggaran aturan strict 1-to-1 linking.
/// Satu akun Keycloak hanya boleh terhubung ke satu karyawan aktif.
/// </summary>
public sealed class DuplicateLinkingException : Exception
{
    public DuplicateLinkingException(string keycloakUserId)
        : base($"Akun Keycloak '{keycloakUserId}' sudah terhubung dengan karyawan aktif lain. " +
               $"Setiap akun hanya boleh ditautkan ke 1 karyawan aktif (Strict 1-to-1 Invariant).") { }

    public DuplicateLinkingException(string keycloakUserId, string existingStaffNip)
        : base($"Akun Keycloak '{keycloakUserId}' sudah terhubung dengan karyawan NIP '{existingStaffNip}'. " +
               $"Lepaskan tautan terlebih dahulu sebelum menautkan ke karyawan lain.") { }
}
