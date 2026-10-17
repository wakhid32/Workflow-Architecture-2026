namespace WorkflowEngine.Application.Common.Exceptions;

/// <summary>
/// Exception yang dilempar ketika entitas yang dicari tidak ditemukan.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"Entitas \"{name}\" dengan kunci ({key}) tidak ditemukan.")
    {
    }
}
