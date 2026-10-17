namespace UserManagement.Domain.Events;

/// <summary>
/// Domain event: Dipancarkan ketika karyawan dinonaktifkan.
/// Handler harus melakukan cascade: blacklist semua token aktif,
/// disable akun Keycloak, dan invalidasi cache terkait.
/// </summary>
public sealed record StaffDeactivatedEvent(
    long StaffId,
    string Nip,
    string? KeycloakUserId,
    string Reason,
    DateTime OccurredAt)
{
    public static StaffDeactivatedEvent Create(
        long staffId, string nip, string? keycloakUserId, string reason)
        => new(staffId, nip, keycloakUserId, reason, DateTime.UtcNow);
}
