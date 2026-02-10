using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IRequirementService
{
    Task<IReadOnlyList<Requirement>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default);
    Task<Requirement?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Requirement> UpsertAsync(UserScope scope, Requirement requirement, CancellationToken cancellationToken = default);
}
