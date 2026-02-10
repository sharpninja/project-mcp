using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IStandardService
{
    Task<IReadOnlyList<Standard>> ListEnterpriseAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Standard>> ListProjectAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default);
    Task<Standard?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Standard> UpsertAsync(UserScope scope, Standard standard, CancellationToken cancellationToken = default);
}
