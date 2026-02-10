using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IWorkItemService
{
    Task<IReadOnlyList<WorkItem>> ListAsync(UserScope scope, WorkItemFilter filter, CancellationToken cancellationToken = default);
    Task<WorkItem?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<WorkItem> UpsertAsync(UserScope scope, WorkItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
}
