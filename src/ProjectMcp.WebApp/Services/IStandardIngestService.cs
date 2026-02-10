using ProjectMcp.WebApp.Models;

namespace ProjectMcp.WebApp.Services;

/// <summary>Ingests standards from JSON documents (e.g. prepared by an external AI).</summary>
public interface IStandardIngestService
{
    /// <summary>Parse JSON array of { "title", "description"? } and create standards in the given enterprise (and optional project). Returns created count and any errors.</summary>
    Task<IngestResult> IngestAsync(UserScope scope, Guid enterpriseId, Guid? projectId, string json, CancellationToken cancellationToken = default);
}
