using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

/// <summary>Scope is resolved only from the database (project_resources). Optional config default when user has no identity.</summary>
public sealed class UserScopeService : IUserScopeService
{
    private readonly IResourceRepository _resources;
    private readonly IProjectResourceRepository _projectResources;
    private readonly ScopeOptions _options;

    public UserScopeService(
        IResourceRepository resources,
        IProjectResourceRepository projectResources,
        IOptions<ScopeOptions> options)
    {
        _resources = resources;
        _projectResources = projectResources;
        _options = options.Value;
    }

    public async Task<UserScope> GetScopeAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        // Resolve resource from DB only (no dependency on OAuth scope claims)
        var resource = await ResolveResourceAsync(user, cancellationToken);
        if (resource is null)
        {
            return new UserScope(Array.Empty<Guid>(), Array.Empty<Guid>(), null);
        }

        var (enterpriseIds, projectIds) = await _projectResources.GetScopeForResourceAsync(resource.Id, cancellationToken);
        return new UserScope(enterpriseIds, projectIds, resource.Id);
    }

    private async Task<Resource?> ResolveResourceAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        // 1) Optional: resolve by OAuth2 subject (NameIdentifier) if present
        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var bySubject = await _resources.ResolveByOAuth2SubAsync(subject, cancellationToken);
            if (bySubject is not null)
                return bySubject;
        }

        // 2) Fallback: configured default resource id
        if (_options.DefaultResourceId.HasValue)
        {
            var byId = await _resources.GetByIdAsync(_options.DefaultResourceId.Value, cancellationToken);
            if (byId is not null)
                return byId;
        }

        // 3) Fallback: configured default OAuth2 sub (e.g. dev user name)
        if (!string.IsNullOrWhiteSpace(_options.DefaultOAuth2Sub))
        {
            var bySub = await _resources.ResolveByOAuth2SubAsync(_options.DefaultOAuth2Sub, cancellationToken);
            if (bySub is not null)
                return bySub;
        }

        return null;
    }
}

/// <summary>Config for scope resolution when not using OAuth claims. Keys: Scope:DefaultResourceId, Scope:DefaultOAuth2Sub.</summary>
public sealed class ScopeOptions
{
    public const string SectionName = "Scope";
    public Guid? DefaultResourceId { get; set; }
    public string? DefaultOAuth2Sub { get; set; }
}
