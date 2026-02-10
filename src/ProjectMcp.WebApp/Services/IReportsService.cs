using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

public interface IReportsService
{
    Task<IReadOnlyList<ReportSummary>> GetProjectSummariesAsync(UserScope scope, CancellationToken cancellationToken = default);
}
