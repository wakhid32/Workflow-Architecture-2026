namespace UserManagement.Application.Common;

/// <summary>
/// Format response standar enterprise untuk seluruh endpoint API.
/// Konsisten di seluruh modul, dilengkapi pesan dan status.
/// </summary>
public sealed class BaseResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }

    public static BaseResponse<T> Ok(T data, string message = "Operasi berhasil.")
        => new() { Success = true, Message = message, Data = data };

    public static BaseResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}
