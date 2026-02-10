using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IAssetService
{
    Task<IReadOnlyList<Asset>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Asset?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Asset> UpsertAsync(UserScope scope, Asset asset, CancellationToken cancellationToken = default);
}
