using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class IssueRepository : IIssueRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public IssueRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Issues.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Issue>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Issues
            .Where(i => i.ProjectId == projectId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Issue> AddAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        _dbContext.Issues.Add(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return issue;
    }

    public async Task<Issue> UpdateAsync(Issue issue, CancellationToken cancellationToken = default)
    {
        _dbContext.Issues.Update(issue);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return issue;
    }
}
