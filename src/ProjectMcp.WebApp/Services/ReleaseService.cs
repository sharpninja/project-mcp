using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class ReleaseService : IReleaseService
{
    private readonly IReleaseRepository _releases;

    public ReleaseService(IReleaseRepository releases)
    {
        _releases = releases;
    }

    public Task<IReadOnlyList<Release>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, projectId);
        return _releases.ListByProjectAsync(projectId, cancellationToken);
    }

    public async Task<Release?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var release = await _releases.GetByIdAsync(id, cancellationToken);
        if (release is null)
        {
            return null;
        }

        ScopeValidation.EnsureProject(scope, release.ProjectId);
        return release;
    }

    public async Task<Release> UpsertAsync(UserScope scope, Release release, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, release.ProjectId);
        return release.Id == Guid.Empty
            ? await _releases.AddAsync(release, cancellationToken)
            : await _releases.UpdateAsync(release, cancellationToken);
    }
}
