using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class StandardRepository : IStandardRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public StandardRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Standard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Standards.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Standard>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Standards
            .Where(s => s.EnterpriseId == enterpriseId && s.ProjectId == null)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Standard>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Standards
            .Where(s => s.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Standard> AddAsync(Standard standard, CancellationToken cancellationToken = default)
    {
        _dbContext.Standards.Add(standard);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return standard;
    }

    public async Task<Standard> UpdateAsync(Standard standard, CancellationToken cancellationToken = default)
    {
        _dbContext.Standards.Update(standard);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return standard;
    }
}
