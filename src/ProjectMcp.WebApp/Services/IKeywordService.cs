using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public interface IKeywordService
{
    Task<IReadOnlyList<Keyword>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Keyword?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default);
    Task<Keyword> UpsertAsync(UserScope scope, Keyword keyword, CancellationToken cancellationToken = default);
}
