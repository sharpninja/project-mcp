namespace ProjectMcp.WebApp.Models;

/// <summary>Result of an ingest operation (requirements or standards from JSON).</summary>
public sealed record IngestResult(int CreatedCount, IReadOnlyList<string> Errors, int KeywordsCreatedCount = 0);
