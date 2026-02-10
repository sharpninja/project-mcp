namespace ProjectMCP.TodoEngine.Abstractions;

public sealed record AuditContext(string? SessionId, Guid? ResourceId, string? CorrelationId);
