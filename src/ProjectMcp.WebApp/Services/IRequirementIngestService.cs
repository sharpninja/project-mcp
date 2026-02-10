using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

/// <summary>Ingests requirements from JSON documents (e.g. prepared by an external AI).</summary>
public interface IRequirementIngestService
{
    /// <summary>Parse JSON array of { "title", "description"? } and create requirements in the given project. Returns created count and any errors.</summary>
    Task<IngestResult> IngestAsync(UserScope scope, Guid projectId, string json, CancellationToken cancellationToken = default);
}
