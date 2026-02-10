using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IIssueService
{
    Task<IReadOnlyList<Issue>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default);
    Task<Issue?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Issue> UpsertAsync(UserScope scope, Issue issue, CancellationToken cancellationToken = default);
}
