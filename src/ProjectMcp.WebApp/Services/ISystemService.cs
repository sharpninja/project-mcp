using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface ISystemService
{
    Task<IReadOnlyList<SystemEntity>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<SystemEntity?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<SystemEntity> UpsertAsync(UserScope scope, SystemEntity system, CancellationToken cancellationToken = default);
}
