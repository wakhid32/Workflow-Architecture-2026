namespace WorkflowEngine.Application.Common.Exceptions;

/// <summary>
/// Exception yang dilempar saat terjadi benturan data konkuren (optimistic concurrency conflict).
/// </summary>
public sealed class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message)
    {
    }

    public ConcurrencyException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
