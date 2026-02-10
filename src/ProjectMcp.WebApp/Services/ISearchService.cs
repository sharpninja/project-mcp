using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

public interface ISearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(UserScope scope, string query, CancellationToken cancellationToken = default);
}
