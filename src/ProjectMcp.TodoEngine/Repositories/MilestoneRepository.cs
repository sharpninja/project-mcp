using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class MilestoneRepository : IMilestoneRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public MilestoneRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Milestones.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Milestone>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Milestones
            .Where(m => m.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Milestone> AddAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        _dbContext.Milestones.Add(milestone);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return milestone;
    }

    public async Task<Milestone> UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default)
    {
        _dbContext.Milestones.Update(milestone);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return milestone;
    }
}
