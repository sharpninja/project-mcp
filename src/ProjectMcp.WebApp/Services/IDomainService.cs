using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IDomainService
{
    Task<IReadOnlyList<Domain>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Domain?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Domain> UpsertAsync(UserScope scope, Domain domain, CancellationToken cancellationToken = default);
}
