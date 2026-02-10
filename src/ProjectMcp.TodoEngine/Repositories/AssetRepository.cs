using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class AssetRepository : IAssetRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public AssetRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Assets.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Asset>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Assets
            .Where(a => a.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Asset> AddAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _dbContext.Assets.Add(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return asset;
    }

    public async Task<Asset> UpdateAsync(Asset asset, CancellationToken cancellationToken = default)
    {
        _dbContext.Assets.Update(asset);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return asset;
    }
}
