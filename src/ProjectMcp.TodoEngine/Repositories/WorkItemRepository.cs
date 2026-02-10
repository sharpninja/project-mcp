using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class WorkItemRepository : IWorkItemRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public WorkItemRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
    }

    public Task<WorkItem?> GetBySlugAsync(string slug, Guid projectId, CancellationToken cancellationToken = default)
    {
        return _dbContext.WorkItems.FirstOrDefaultAsync(w => w.DisplayId == slug && w.ProjectId == projectId, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkItem>> ListAsync(WorkItemFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.WorkItems.AsQueryable();

        if (filter.ProjectId.HasValue)
        {
            query = query.Where(w => w.ProjectId == filter.ProjectId.Value);
        }

        if (filter.ParentId.HasValue)
        {
            query = query.Where(w => w.ParentId == filter.ParentId.Value);
        }

        if (filter.Level.HasValue)
        {
            query = query.Where(w => w.Level == filter.Level.Value);
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(w => w.Status == filter.Status.Value);
        }

        if (filter.MilestoneId.HasValue)
        {
            query = query.Where(w => w.MilestoneId == filter.MilestoneId.Value);
        }

        if (filter.ResourceId.HasValue)
        {
            query = query.Where(w => w.ResourceId == filter.ResourceId.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<WorkItem> AddAsync(WorkItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkItems.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<WorkItem> UpdateAsync(WorkItem item, CancellationToken cancellationToken = default)
    {
        _dbContext.WorkItems.Update(item);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return item;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.WorkItems.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        _dbContext.WorkItems.Remove(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
