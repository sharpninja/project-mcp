using GPS.SimpleMVC.Views;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Abstractions;

public interface ITodoView : ISimpleView
{
    Task<Project?> GetProjectAsync(ScopeContext scope, CancellationToken cancellationToken = default);
    Task<Project> UpsertProjectAsync(ScopeContext scope, Project project, AuditContext? audit, CancellationToken cancellationToken = default);
    Task<WorkItem?> GetWorkItemAsync(ScopeContext scope, Guid id, CancellationToken cancellationToken = default);
    Task<WorkItem> CreateWorkItemAsync(ScopeContext scope, WorkItem item, AuditContext? audit, CancellationToken cancellationToken = default);
    Task<WorkItem> UpdateWorkItemAsync(ScopeContext scope, WorkItem item, AuditContext? audit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItem>> ListWorkItemsAsync(ScopeContext scope, WorkItemFilter filter, CancellationToken cancellationToken = default);
    Task<bool> DeleteWorkItemAsync(ScopeContext scope, Guid id, AuditContext? audit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Milestone>> ListMilestonesAsync(ScopeContext scope, CancellationToken cancellationToken = default);
    Task<Milestone> UpsertMilestoneAsync(ScopeContext scope, Milestone milestone, AuditContext? audit, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Release>> ListReleasesAsync(ScopeContext scope, CancellationToken cancellationToken = default);
    Task<Release> UpsertReleaseAsync(ScopeContext scope, Release release, AuditContext? audit, CancellationToken cancellationToken = default);
}
