namespace ProjectMcp.WebApp.Models;

public sealed record UserScope(
    IReadOnlyList<Guid> AllowedEnterpriseIds,
    IReadOnlyList<Guid> AllowedProjectIds,
    Guid? CurrentResourceId);

/// <summary>Resolved once per request and cascaded so only one component uses DbContext for scope.</summary>
public sealed record ScopeState(bool IsLoading, UserScope? Scope);
