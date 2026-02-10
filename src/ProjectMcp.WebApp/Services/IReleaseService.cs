using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IReleaseService
{
    Task<IReadOnlyList<Release>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default);
    Task<Release?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Release> UpsertAsync(UserScope scope, Release release, CancellationToken cancellationToken = default);
}
