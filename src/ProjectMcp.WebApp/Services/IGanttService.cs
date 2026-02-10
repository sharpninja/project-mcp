using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

public interface IGanttService
{
    Task<IReadOnlyList<GanttItem>> GetItemsAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default);
}
