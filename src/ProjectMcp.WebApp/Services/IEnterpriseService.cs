using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IEnterpriseService
{
    Task<IReadOnlyList<Enterprise>> ListAsync(UserScope scope, CancellationToken cancellationToken = default);
    Task<Enterprise?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
}
