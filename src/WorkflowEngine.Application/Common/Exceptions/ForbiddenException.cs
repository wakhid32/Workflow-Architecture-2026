namespace WorkflowEngine.Application.Common.Exceptions;

/// <summary>
/// Exception yang dilempar saat user tidak memiliki hak akses/otoritas approval.
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
