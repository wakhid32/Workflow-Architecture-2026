namespace UserManagement.Domain.Exceptions;

/// <summary>
/// Exception dilemparkan ketika staf dengan ID, NIP, atau Email yang dimaksud tidak ditemukan.
/// </summary>
public sealed class StaffNotFoundException : Exception
{
    public StaffNotFoundException(string identifier)
        : base($"Data staf dengan identifier '{identifier}' tidak ditemukan di database security.") { }

    public StaffNotFoundException(long staffId)
        : base($"Data staf dengan ID '{staffId}' tidak ditemukan di database security.") { }
}
