namespace WorkflowEngine.Application.Common.Exceptions;

/// <summary>
/// Exception khusus untuk kesalahan logika bisnis workflow.
/// </summary>
public sealed class WorkflowException : Exception
{
    public WorkflowException(string message) : base(message) { }
    public WorkflowException(string message, Exception innerException) : base(message, innerException) { }
}
