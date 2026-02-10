using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class IssueService : IIssueService
{
    private readonly IIssueRepository _issues;

    public IssueService(IIssueRepository issues)
    {
        _issues = issues;
    }

    public Task<IReadOnlyList<Issue>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, projectId);
        return _issues.ListByProjectAsync(projectId, cancellationToken);
    }

    public async Task<Issue?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var issue = await _issues.GetByIdAsync(id, cancellationToken);
        if (issue is null)
        {
            return null;
        }

        ScopeValidation.EnsureProject(scope, issue.ProjectId);
        return issue;
    }

    public async Task<Issue> UpsertAsync(UserScope scope, Issue issue, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, issue.ProjectId);
        return issue.Id == Guid.Empty
            ? await _issues.AddAsync(issue, cancellationToken)
            : await _issues.UpdateAsync(issue, cancellationToken);
    }
}
