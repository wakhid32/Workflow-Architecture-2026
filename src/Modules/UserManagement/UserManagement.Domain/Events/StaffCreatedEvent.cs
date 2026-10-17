namespace UserManagement.Domain.Events;

/// <summary>
/// Domain event: Dipancarkan ketika data staf baru berhasil dibuat
/// atau di-provision bersama akun Keycloak.
/// </summary>
public sealed record StaffCreatedEvent(
    long StaffId,
    string Nip,
    string NamaLengkap,
    string Email,
    string KodeDepartemen,
    string KodeJabatan,
    string? KeycloakUserId,
    DateTime OccurredAt)
{
    public static StaffCreatedEvent Create(
        long staffId, string nip, string namaLengkap, string email,
        string kodeDepartemen, string kodeJabatan, string? keycloakUserId)
        => new(staffId, nip, namaLengkap, email, kodeDepartemen, kodeJabatan, keycloakUserId, DateTime.UtcNow);
}
