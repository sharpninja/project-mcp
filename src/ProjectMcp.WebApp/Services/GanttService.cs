using Microsoft.EntityFrameworkCore;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Data;

namespace ProjectMcp.WebApp.Services;

public sealed class GanttService : IGanttService
{
    private readonly TodoEngineDbContext _dbContext;

    public GanttService(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<GanttItem>> GetItemsAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, projectId);

        var items = await _dbContext.WorkItems
            .Where(w => w.ProjectId == projectId)
            .Select(w => new GanttItem(w.Id, w.Title, w.StartDate, w.DueDate))
            .ToListAsync(cancellationToken);

        return items;
    }
}
