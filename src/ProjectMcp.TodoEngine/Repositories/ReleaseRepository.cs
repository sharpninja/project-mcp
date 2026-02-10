using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class ReleaseRepository : IReleaseRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public ReleaseRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Releases.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Release>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Releases
            .Where(r => r.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Release> AddAsync(Release release, CancellationToken cancellationToken = default)
    {
        _dbContext.Releases.Add(release);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return release;
    }

    public async Task<Release> UpdateAsync(Release release, CancellationToken cancellationToken = default)
    {
        _dbContext.Releases.Update(release);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return release;
    }
}
