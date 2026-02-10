using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Exceptions;
using ProjectMCP.TodoEngine.Models;
using Serilog;

namespace ProjectMCP.TodoEngine.Views;

public sealed class TodoView : ITodoView
{
    private static readonly ILogger Log = Serilog.Log.ForContext<TodoView>();
    public Guid ViewKey { get; } = Guid.Parse("f2b2f9c7-1a0f-4a9d-8a3a-9903ff1b0d0e");
    private readonly IProjectRepository _projects;
    private readonly IWorkItemRepository _workItems;
    private readonly IMilestoneRepository _milestones;
    private readonly IReleaseRepository _releases;

    public TodoView(
        IProjectRepository projects,
        IWorkItemRepository workItems,
        IMilestoneRepository milestones,
        IReleaseRepository releases)
    {
        _projects = projects;
        _workItems = workItems;
        _milestones = milestones;
        _releases = releases;
    }

    public async Task<Project?> GetProjectAsync(ScopeContext scope, CancellationToken cancellationToken = default)
    {
        if (!scope.ProjectId.HasValue)
        {
            return null;
        }

        var project = await _projects.GetByIdAsync(scope.ProjectId.Value, cancellationToken);
        if (project is not null && project.EnterpriseId != scope.EnterpriseId)
        {
            Log.Warning("Scope violation for project {ProjectId} enterprise {EnterpriseId} in scope {ScopeEnterpriseId}", project.Id, project.EnterpriseId, scope.EnterpriseId);
            throw new ScopeViolationException("Project is outside the current enterprise scope.");
        }

        return project;
    }

    public async Task<Project> UpsertProjectAsync(ScopeContext scope, Project project, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        project.EnterpriseId = scope.EnterpriseId;
        project.Id = project.Id == Guid.Empty ? Guid.NewGuid() : project.Id;
        project.CreatedAt = project.CreatedAt == default ? DateTimeOffset.UtcNow : project.CreatedAt;
        project.UpdatedAt = DateTimeOffset.UtcNow;

        return await _projects.ExistsAsync(project.Id, cancellationToken)
            ? await _projects.UpdateAsync(project, cancellationToken)
            : await _projects.AddAsync(project, cancellationToken);
    }

    public Task<WorkItem?> GetWorkItemAsync(ScopeContext scope, Guid id, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        return GetScopedWorkItemAsync(scope, id, cancellationToken);
    }

    public async Task<WorkItem> CreateWorkItemAsync(ScopeContext scope, WorkItem item, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        item.ProjectId = scope.ProjectId!.Value;
        item.Id = item.Id == Guid.Empty ? Guid.NewGuid() : item.Id;
        item.CreatedAt = item.CreatedAt == default ? DateTimeOffset.UtcNow : item.CreatedAt;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        return await _workItems.AddAsync(item, cancellationToken);
    }

    public async Task<WorkItem> UpdateWorkItemAsync(ScopeContext scope, WorkItem item, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        if (item.ProjectId != scope.ProjectId)
        {
            Log.Warning("Scope violation for work item {WorkItemId} project {ProjectId} in scope {ScopeProjectId}", item.Id, item.ProjectId, scope.ProjectId);
            throw new ScopeViolationException("Work item is outside the current project scope.");
        }

        item.UpdatedAt = DateTimeOffset.UtcNow;
        return await _workItems.UpdateAsync(item, cancellationToken);
    }

    public Task<IReadOnlyList<WorkItem>> ListWorkItemsAsync(ScopeContext scope, WorkItemFilter filter, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        var scopedFilter = new WorkItemFilter
        {
            ProjectId = scope.ProjectId,
            ParentId = filter.ParentId,
            Level = filter.Level,
            Status = filter.Status,
            MilestoneId = filter.MilestoneId,
            ResourceId = filter.ResourceId
        };
        return _workItems.ListAsync(scopedFilter, cancellationToken);
    }

    public async Task<bool> DeleteWorkItemAsync(ScopeContext scope, Guid id, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        var existing = await GetScopedWorkItemAsync(scope, id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        if (existing.ProjectId != scope.ProjectId)
        {
            Log.Warning("Scope violation for work item {WorkItemId} project {ProjectId} in scope {ScopeProjectId}", existing.Id, existing.ProjectId, scope.ProjectId);
            throw new ScopeViolationException("Work item is outside the current project scope.");
        }

        return await _workItems.DeleteAsync(id, cancellationToken);
    }

    public Task<IReadOnlyList<Milestone>> ListMilestonesAsync(ScopeContext scope, CancellationToken cancellationToken = default)
    {
        return _milestones.ListByEnterpriseAsync(scope.EnterpriseId, cancellationToken);
    }

    public async Task<Milestone> UpsertMilestoneAsync(ScopeContext scope, Milestone milestone, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        milestone.EnterpriseId = scope.EnterpriseId;
        milestone.Id = milestone.Id == Guid.Empty ? Guid.NewGuid() : milestone.Id;
        milestone.CreatedAt = milestone.CreatedAt == default ? DateTimeOffset.UtcNow : milestone.CreatedAt;
        milestone.UpdatedAt = DateTimeOffset.UtcNow;

        var existing = await _milestones.GetByIdAsync(milestone.Id, cancellationToken);
        return existing is null
            ? await _milestones.AddAsync(milestone, cancellationToken)
            : await _milestones.UpdateAsync(milestone, cancellationToken);
    }

    public Task<IReadOnlyList<Release>> ListReleasesAsync(ScopeContext scope, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        return _releases.ListByProjectAsync(scope.ProjectId!.Value, cancellationToken);
    }

    public async Task<Release> UpsertReleaseAsync(ScopeContext scope, Release release, AuditContext? audit, CancellationToken cancellationToken = default)
    {
        EnsureProjectScope(scope);
        release.ProjectId = scope.ProjectId!.Value;
        release.Id = release.Id == Guid.Empty ? Guid.NewGuid() : release.Id;
        release.CreatedAt = release.CreatedAt == default ? DateTimeOffset.UtcNow : release.CreatedAt;
        release.UpdatedAt = DateTimeOffset.UtcNow;

        var existing = await _releases.GetByIdAsync(release.Id, cancellationToken);
        return existing is null
            ? await _releases.AddAsync(release, cancellationToken)
            : await _releases.UpdateAsync(release, cancellationToken);
    }

    private static void EnsureProjectScope(ScopeContext scope)
    {
        if (!scope.ProjectId.HasValue)
        {
            Log.Warning("Project scope missing for enterprise {EnterpriseId}", scope.EnterpriseId);
            throw new ScopeViolationException("Project scope is required for this operation.");
        }
    }

    private async Task<WorkItem?> GetScopedWorkItemAsync(ScopeContext scope, Guid id, CancellationToken cancellationToken)
    {
        var item = await _workItems.GetByIdAsync(id, cancellationToken);
        if (item is not null && item.ProjectId != scope.ProjectId)
        {
            Log.Warning("Scope violation for work item {WorkItemId} project {ProjectId} in scope {ScopeProjectId}", item.Id, item.ProjectId, scope.ProjectId);
            throw new ScopeViolationException("Work item is outside the current project scope.");
        }

        return item;
    }
}
