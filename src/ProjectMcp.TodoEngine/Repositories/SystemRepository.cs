using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class SystemRepository : ISystemRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public SystemRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<SystemEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Systems.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<SystemEntity>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Systems
            .Where(s => s.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<SystemEntity> AddAsync(SystemEntity system, CancellationToken cancellationToken = default)
    {
        _dbContext.Systems.Add(system);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return system;
    }

    public async Task<SystemEntity> UpdateAsync(SystemEntity system, CancellationToken cancellationToken = default)
    {
        _dbContext.Systems.Update(system);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return system;
    }
}
