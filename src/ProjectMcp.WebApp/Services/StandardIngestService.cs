using System.Text.Json;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class StandardIngestService : IStandardIngestService
{
    private readonly IStandardService _standards;
    private readonly IEnterpriseService _enterprises;
    private readonly ISlugService _slugService;
    private readonly ILogger<StandardIngestService> _logger;

    public StandardIngestService(
        IStandardService standards,
        IEnterpriseService enterprises,
        ISlugService slugService,
        ILogger<StandardIngestService> logger)
    {
        _standards = standards;
        _enterprises = enterprises;
        _slugService = slugService;
        _logger = logger;
    }

    public async Task<IngestResult> IngestAsync(UserScope scope, Guid enterpriseId, Guid? projectId, string json, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        _logger.LogInformation("Standard ingest started. EnterpriseId={EnterpriseId}, ProjectId={ProjectId}, JsonLength={JsonLength}", enterpriseId, projectId, json?.Length ?? 0);

        var enterprise = await _enterprises.GetAsync(scope, enterpriseId, cancellationToken);
        if (enterprise is null)
        {
            _logger.LogWarning("Standard ingest aborted: enterprise not found or not in scope. EnterpriseId={EnterpriseId}", enterpriseId);
            return new IngestResult(0, new[] { "Enterprise not found or not in scope." });
        }

        if (projectId.HasValue && !scope.AllowedProjectIds.Contains(projectId.Value))
        {
            _logger.LogWarning("Standard ingest aborted: project not in scope. ProjectId={ProjectId}", projectId);
            return new IngestResult(0, new[] { "Project not in scope." });
        }

        List<StandardIngestItem>? items;
        try
        {
            items = JsonSerializer.Deserialize<List<StandardIngestItem>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Standard ingest: invalid JSON");
            return new IngestResult(0, new[] { $"Invalid JSON: {ex.Message}" });
        }

        if (items is null || items.Count == 0)
        {
            _logger.LogWarning("Standard ingest: no items in JSON or empty list.");
            return new IngestResult(0, errors);
        }

        _logger.LogInformation("Standard ingest: processing {Count} standard(s)", items.Count);
        var created = 0;
        var now = DateTimeOffset.UtcNow;
        var ownerSlug = enterprise.DisplayId;
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            var title = item.Title?.Trim();
            if (string.IsNullOrWhiteSpace(title))
            {
                errors.Add("Skipped item with empty title.");
                continue;
            }

            try
            {
                var displayId = await _slugService.AllocateSlugAsync(SlugEntityType.Standard, ownerSlug, cancellationToken);
                var standard = new Standard
                {
                    Id = Guid.NewGuid(),
                    DisplayId = displayId,
                    EnterpriseId = enterpriseId,
                    ProjectId = projectId,
                    Title = title,
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await _standards.UpsertAsync(scope, standard, cancellationToken);
                created++;
                if ((i + 1) % 10 == 0 || i == items.Count - 1)
                    _logger.LogInformation("Standard ingest progress: {Current}/{Total} items, {Created} created", i + 1, items.Count, created);
            }
            catch (Exception ex)
            {
                errors.Add($"{title}: {ex.Message}");
                _logger.LogWarning(ex, "Standard ingest item failed: {Title}", title);
            }
        }

        _logger.LogInformation("Standard ingest completed. Created={Created}, Errors={ErrorCount}", created, errors.Count);
        return new IngestResult(created, errors);
    }

    private sealed class StandardIngestItem
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }
}
