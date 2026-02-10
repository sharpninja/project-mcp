using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IProjectService
{
    Task<IReadOnlyList<Project>> ListAsync(UserScope scope, CancellationToken cancellationToken = default);
    Task<Project?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Project> UpsertAsync(UserScope scope, Project project, CancellationToken cancellationToken = default);
}
