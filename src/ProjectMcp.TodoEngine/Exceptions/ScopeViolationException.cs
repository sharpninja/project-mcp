namespace ProjectMCP.TodoEngine.Exceptions;

public sealed class ScopeViolationException : InvalidOperationException
{
    public ScopeViolationException(string message)
        : base(message)
    {
    }
}
