namespace ProjectMCP.TodoEngine;

public sealed class TodoEngineOptions
{
    public TodoEngineProvider? Provider { get; set; }
    public string? ConnectionString { get; set; }
}
