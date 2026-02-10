using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class WorkItemService : IWorkItemService
{
    private readonly IWorkItemRepository _workItems;

    public WorkItemService(IWorkItemRepository workItems)
    {
        _workItems = workItems;
    }

    public Task<IReadOnlyList<WorkItem>> ListAsync(UserScope scope, WorkItemFilter filter, CancellationToken cancellationToken = default)
    {
        if (filter.ProjectId.HasValue)
        {
            ScopeValidation.EnsureProject(scope, filter.ProjectId.Value);
        }
        else if (scope.AllowedProjectIds.Count == 1)
        {
            filter = new WorkItemFilter
            {
                ProjectId = scope.AllowedProjectIds[0],
                ParentId = filter.ParentId,
                Level = filter.Level,
                Status = filter.Status,
                MilestoneId = filter.MilestoneId,
                ResourceId = filter.ResourceId
            };
        }

        return _workItems.ListAsync(filter, cancellationToken);
    }

    public async Task<WorkItem?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _workItems.GetByIdAsync(id, cancellationToken);
        if (item is null)
        {
            return null;
        }

        ScopeValidation.EnsureProject(scope, item.ProjectId);
        return item;
    }

    public async Task<WorkItem> UpsertAsync(UserScope scope, WorkItem item, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, item.ProjectId);
        return item.Id == Guid.Empty
            ? await _workItems.AddAsync(item, cancellationToken)
            : await _workItems.UpdateAsync(item, cancellationToken);
    }

    public Task<bool> DeleteAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        return DeleteScopedAsync(scope, id, cancellationToken);
    }

    private async Task<bool> DeleteScopedAsync(UserScope scope, Guid id, CancellationToken cancellationToken)
    {
        var existing = await _workItems.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        ScopeValidation.EnsureProject(scope, existing.ProjectId);
        return await _workItems.DeleteAsync(id, cancellationToken);
    }
}
