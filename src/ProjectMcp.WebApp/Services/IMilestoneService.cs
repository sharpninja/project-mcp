using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IMilestoneService
{
    Task<IReadOnlyList<Milestone>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Milestone?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Milestone> UpsertAsync(UserScope scope, Milestone milestone, CancellationToken cancellationToken = default);
}
