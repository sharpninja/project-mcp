using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IResourceService
{
    Task<IReadOnlyList<Resource>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Resource?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
}
