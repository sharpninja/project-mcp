namespace ProjectMcp.WebApp.Models;

public sealed record SearchResult(string EntityType, Guid Id, string Title, string? Snippet);
