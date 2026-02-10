using Microsoft.EntityFrameworkCore;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Data;

namespace ProjectMcp.WebApp.Services;

public sealed class SearchService : ISearchService
{
    private readonly TodoEngineDbContext _dbContext;

    public SearchService(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(UserScope scope, string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query) || scope.AllowedProjectIds.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var term = query.Trim();
        var projectIds = scope.AllowedProjectIds;

        var projects = await _dbContext.Projects
            .Where(p => projectIds.Contains(p.Id) && (p.Name.Contains(term) || (p.Description != null && p.Description.Contains(term))))
            .Select(p => new SearchResult("Project", p.Id, p.Name, p.Description))
            .ToListAsync(cancellationToken);

        var requirements = await _dbContext.Requirements
            .Where(r => projectIds.Contains(r.ProjectId) && (r.Title.Contains(term) || (r.Description != null && r.Description.Contains(term))))
            .Select(r => new SearchResult("Requirement", r.Id, r.Title, r.Description))
            .ToListAsync(cancellationToken);

        var workItems = await _dbContext.WorkItems
            .Where(w => projectIds.Contains(w.ProjectId) && (w.Title.Contains(term) || (w.Description != null && w.Description.Contains(term))))
            .Select(w => new SearchResult("WorkItem", w.Id, w.Title, w.Description))
            .ToListAsync(cancellationToken);

        var issues = await _dbContext.Issues
            .Where(i => projectIds.Contains(i.ProjectId) && (i.Title.Contains(term) || (i.Description != null && i.Description.Contains(term))))
            .Select(i => new SearchResult("Issue", i.Id, i.Title, i.Description))
            .ToListAsync(cancellationToken);

        return projects
            .Concat(requirements)
            .Concat(workItems)
            .Concat(issues)
            .ToList();
    }
}
