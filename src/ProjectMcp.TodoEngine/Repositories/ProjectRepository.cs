using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class ProjectRepository : IProjectRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public ProjectRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Projects.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public Task<Project?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Projects.FirstOrDefaultAsync(p => p.DisplayId == slug && p.EnterpriseId == enterpriseId, cancellationToken);
    }

    public async Task<IReadOnlyList<Project>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Projects
            .Where(p => p.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Add(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public async Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken = default)
    {
        _dbContext.Projects.Update(project);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return project;
    }

    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Projects.AnyAsync(p => p.Id == id, cancellationToken);
    }
}
