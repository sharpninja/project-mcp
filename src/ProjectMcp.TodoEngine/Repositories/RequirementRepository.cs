using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class RequirementRepository : IRequirementRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public RequirementRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Requirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Requirements.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Requirement>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Requirements
            .Include(r => r.Keyword)
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Requirement> AddAsync(Requirement requirement, CancellationToken cancellationToken = default)
    {
        _dbContext.Requirements.Add(requirement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return requirement;
    }

    public async Task<Requirement> UpdateAsync(Requirement requirement, CancellationToken cancellationToken = default)
    {
        _dbContext.Requirements.Update(requirement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return requirement;
    }
}
