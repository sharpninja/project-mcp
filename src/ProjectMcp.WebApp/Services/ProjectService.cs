using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class ProjectService : IProjectService
{
    private readonly IProjectRepository _projects;

    public ProjectService(IProjectRepository projects)
    {
        _projects = projects;
    }

    public async Task<IReadOnlyList<Project>> ListAsync(UserScope scope, CancellationToken cancellationToken = default)
    {
        var results = new List<Project>();
        foreach (var enterpriseId in scope.AllowedEnterpriseIds)
        {
            var projects = await _projects.ListByEnterpriseAsync(enterpriseId, cancellationToken);
            results.AddRange(projects.Where(p => scope.AllowedProjectIds.Count == 0 || scope.AllowedProjectIds.Contains(p.Id)));
        }

        return results;
    }

    public async Task<Project?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var project = await _projects.GetByIdAsync(id, cancellationToken);
        if (project is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, project.EnterpriseId);
        if (scope.AllowedProjectIds.Count > 0)
        {
            ScopeValidation.EnsureProject(scope, project.Id);
        }

        return project;
    }

    public async Task<Project> UpsertAsync(UserScope scope, Project project, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, project.EnterpriseId);
        return await _projects.ExistsAsync(project.Id, cancellationToken)
            ? await _projects.UpdateAsync(project, cancellationToken)
            : await _projects.AddAsync(project, cancellationToken);
    }
}
