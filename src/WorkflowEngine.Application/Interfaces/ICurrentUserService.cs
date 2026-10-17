namespace WorkflowEngine.Application.Interfaces;

/// <summary>
/// Abstraksi untuk mendapatkan konteks pengguna yang sedang aktif/login.
/// </summary>
public interface ICurrentUserService
{
    long UserId { get; }
    string UserName { get; }
    string? UserNpk { get; }
    string? UserRole { get; }
    string? ClientIpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
}
