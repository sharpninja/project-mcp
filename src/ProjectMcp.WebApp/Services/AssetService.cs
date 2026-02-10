using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class AssetService : IAssetService
{
    private readonly IAssetRepository _assets;

    public AssetService(IAssetRepository assets)
    {
        _assets = assets;
    }

    public Task<IReadOnlyList<Asset>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _assets.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<Asset?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var asset = await _assets.GetByIdAsync(id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, asset.EnterpriseId);
        return asset;
    }

    public async Task<Asset> UpsertAsync(UserScope scope, Asset asset, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, asset.EnterpriseId);
        return asset.Id == Guid.Empty
            ? await _assets.AddAsync(asset, cancellationToken)
            : await _assets.UpdateAsync(asset, cancellationToken);
    }
}
