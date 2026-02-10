using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class ResourceRepository : IResourceRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public ResourceRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public Task<Resource?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.FirstOrDefaultAsync(r => r.DisplayId == slug && r.EnterpriseId == enterpriseId, cancellationToken);
    }

    public Task<Resource?> ResolveAgentNameToResourceAsync(string agentName, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.FirstOrDefaultAsync(
            r => r.EnterpriseId == enterpriseId && (r.Name == agentName || r.OAuth2Sub == agentName),
            cancellationToken);
    }

    public Task<Resource?> ResolveByOAuth2SubAsync(string oauth2Sub, CancellationToken cancellationToken = default)
    {
        return _dbContext.Resources.FirstOrDefaultAsync(
            r => r.OAuth2Sub == oauth2Sub,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Resource>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Resources
            .Where(r => r.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }
}
