using Microsoft.EntityFrameworkCore;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class ReportsService : IReportsService
{
    private readonly TodoEngineDbContext _dbContext;

    public ReportsService(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<ReportSummary>> GetProjectSummariesAsync(UserScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.AllowedProjectIds.Count == 0)
        {
            return Array.Empty<ReportSummary>();
        }

        var summaries = await _dbContext.WorkItems
            .Where(w => w.Level == WorkItemLevel.Task && scope.AllowedProjectIds.Contains(w.ProjectId))
            .GroupBy(w => w.ProjectId)
            .Select(group => new
            {
                ProjectId = group.Key,
                Total = group.Count(),
                Done = group.Count(w => w.Status == WorkItemStatus.Done),
                InProgress = group.Count(w => w.Status == WorkItemStatus.InProgress)
            })
            .ToListAsync(cancellationToken);

        return summaries.Select(s => new ReportSummary(s.ProjectId, s.Total, s.Done, s.InProgress)).ToList();
    }
}
