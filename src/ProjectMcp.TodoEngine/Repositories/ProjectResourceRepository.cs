using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class ProjectResourceRepository : IProjectResourceRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public ProjectResourceRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Guid> EnterpriseIds, IReadOnlyList<Guid> ProjectIds)> GetScopeForResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.ProjectResources
            .Where(pr => pr.ResourceId == resourceId)
            .Join(_dbContext.Projects, pr => pr.ProjectId, p => p.Id, (_, p) => new { p.Id, p.EnterpriseId })
            .ToListAsync(cancellationToken);

        var projectIds = rows.Select(r => r.Id).Distinct().ToList();
        var enterpriseIds = rows.Select(r => r.EnterpriseId).Distinct().ToList();
        return (enterpriseIds, projectIds);
    }
}
