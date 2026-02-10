using System.Text.Json;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class RequirementIngestService : IRequirementIngestService
{
    private readonly IRequirementService _requirements;
    private readonly IProjectService _projects;
    private readonly IKeywordService _keywords;
    private readonly ISlugService _slugService;
    private readonly ILogger<RequirementIngestService> _logger;

    public RequirementIngestService(
        IRequirementService requirements,
        IProjectService projects,
        IKeywordService keywords,
        ISlugService slugService,
        ILogger<RequirementIngestService> logger)
    {
        _requirements = requirements;
        _projects = projects;
        _keywords = keywords;
        _slugService = slugService;
        _logger = logger;
    }

    public async Task<IngestResult> IngestAsync(UserScope scope, Guid projectId, string json, CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();
        _logger.LogInformation("Requirement ingest started. ProjectId={ProjectId}, JsonLength={JsonLength}", projectId, json?.Length ?? 0);

        var project = await _projects.GetAsync(scope, projectId, cancellationToken);
        if (project is null)
        {
            _logger.LogWarning("Requirement ingest aborted: project not found or not in scope. ProjectId={ProjectId}", projectId);
            return new IngestResult(0, new[] { "Project not found or not in scope." });
        }

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // Try document format first: { "keywords": [...], "requirements": [...] }
        var doc = JsonSerializer.Deserialize<RequirementIngestDocument>(json, opts);
        if (doc?.Keywords?.Count > 0 || doc?.Requirements?.Count > 0)
        {
            _logger.LogInformation("Requirement ingest: using document format. Keywords={KeywordCount}, Requirements={RequirementCount}", doc!.Keywords?.Count ?? 0, doc.Requirements?.Count ?? 0);
            return await IngestDocumentAsync(scope, project, doc!, errors, opts, cancellationToken);
        }

        // Legacy: array of { title, description? }
        var items = JsonSerializer.Deserialize<List<RequirementIngestItem>>(json, opts);
        if (items is null || items.Count == 0)
        {
            _logger.LogWarning("Requirement ingest: no items in JSON or invalid format.");
            return new IngestResult(0, errors);
        }

        _logger.LogInformation("Requirement ingest: using legacy array format. ItemCount={Count}", items.Count);
        var existingList = await _requirements.ListAsync(scope, projectId, cancellationToken);
        var byTitle = new Dictionary<string, Requirement>(StringComparer.OrdinalIgnoreCase);
        foreach (var ex in existingList)
        {
            var t = ex.Title?.Trim();
            if (!string.IsNullOrEmpty(t) && !byTitle.ContainsKey(t))
                byTitle[t] = ex;
        }
        var created = 0;
        var now = DateTimeOffset.UtcNow;
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
                Requirement? existing = null;
                string displayId;
                if (byTitle.TryGetValue(title, out existing))
                {
                    displayId = existing.DisplayId;
                }
                else
                {
                    displayId = await _slugService.AllocateSlugAsync(SlugEntityType.Requirement, project.DisplayId, cancellationToken);
                }
                var isUpdate = existing is not null;
                var requirement = new Requirement
                {
                    Id = isUpdate ? existing!.Id : Guid.NewGuid(),
                    DisplayId = displayId,
                    ProjectId = projectId,
                    ParentId = null,
                    Title = title,
                    Description = string.IsNullOrWhiteSpace(item.Description) ? null : item.Description.Trim(),
                    State = RequirementState.Draft,
                    CreatedAt = isUpdate ? existing!.CreatedAt : now,
                    UpdatedAt = now
                };
                await _requirements.UpsertAsync(scope, requirement, cancellationToken);
                if (!isUpdate)
                {
                    created++;
                    byTitle[title] = requirement;
                }
                if ((i + 1) % 10 == 0 || i == items.Count - 1)
                    _logger.LogInformation("Requirement ingest progress: {Current}/{Total} items, {Created} created", i + 1, items.Count, created);
            }
            catch (Exception ex)
            {
                errors.Add($"{title}: {ex.Message}");
                _logger.LogWarning(ex, "Requirement ingest item failed: {Title}", title);
            }
        }

        _logger.LogInformation("Requirement ingest completed. Created={Created}, Errors={ErrorCount}", created, errors.Count);
        return new IngestResult(created, errors);
    }

    private async Task<IngestResult> IngestDocumentAsync(
        UserScope scope,
        Project project,
        RequirementIngestDocument doc,
        List<string> errors,
        JsonSerializerOptions opts,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var keywordsCreated = 0;
        var enterpriseId = project.EnterpriseId;
        var projectId = project.Id;

        // 1) Ensure keyword instances exist for each category
        if (doc.Keywords?.Count > 0)
        {
            _logger.LogInformation("Requirement ingest: processing {Count} keyword(s)", doc.Keywords.Count);
            var existing = await _keywords.ListAsync(scope, enterpriseId, cancellationToken);
            var byDisplayId = existing.ToDictionary(k => k.DisplayId, StringComparer.OrdinalIgnoreCase);
            foreach (var kw in doc.Keywords)
            {
                var displayId = (kw.DisplayId ?? slugFromName(kw.Name)).Trim();
                var name = kw.Name?.Trim() ?? "";
                if (string.IsNullOrEmpty(displayId) || string.IsNullOrEmpty(name))
                {
                    continue;
                }
                if (byDisplayId.ContainsKey(displayId))
                {
                    _logger.LogDebug("Requirement ingest: keyword already exists, skipping. DisplayId={DisplayId}", displayId);
                    continue;
                }
                try
                {
                    var keyword = new Keyword
                    {
                        Id = Guid.NewGuid(),
                        DisplayId = displayId,
                        EnterpriseId = enterpriseId,
                        Name = name,
                        CreatedAt = now,
                        UpdatedAt = now
                    };
                    await _keywords.UpsertAsync(scope, keyword, cancellationToken);
                    byDisplayId[displayId] = keyword;
                    keywordsCreated++;
                    _logger.LogDebug("Requirement ingest: created keyword. DisplayId={DisplayId}, Name={Name}", displayId, name);
                }
                catch (Exception ex)
                {
                    errors.Add($"Keyword '{name}': {ex.Message}");
                    _logger.LogWarning(ex, "Requirement ingest: keyword creation failed. Name={Name}", name);
                }
            }
            _logger.LogInformation("Requirement ingest: keywords phase done. Created={KeywordsCreated}, Errors={ErrorCount}", keywordsCreated, errors.Count);
        }

        // Build keyword id by name (for linking requirements to category keyword)
        var keywordIdByCategoryName = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        var allKeywords = await _keywords.ListAsync(scope, enterpriseId, cancellationToken);
        foreach (var k in allKeywords)
        {
            var n = k.Name?.Trim();
            if (!string.IsNullOrEmpty(n) && !keywordIdByCategoryName.ContainsKey(n))
            {
                keywordIdByCategoryName[n] = k.Id;
            }
        }

        // 2) Upsert requirements by (projectId, displayId) to avoid duplicate key and circular dependency.
        // When the document omits displayId, match existing by title so re-ingesting the same data updates instead of creating duplicates.
        var requirementsCreated = 0;
        if (doc.Requirements?.Count > 0)
        {
            _logger.LogInformation("Requirement ingest: processing {Count} requirement(s)", doc.Requirements.Count);
            var existingList = await _requirements.ListAsync(scope, projectId, cancellationToken);
            var byDisplayId = existingList.ToDictionary(x => x.DisplayId, StringComparer.OrdinalIgnoreCase);
            var byTitle = new Dictionary<string, Requirement>(StringComparer.OrdinalIgnoreCase);
            foreach (var ex in existingList)
            {
                var t = ex.Title?.Trim();
                if (!string.IsNullOrEmpty(t) && !byTitle.ContainsKey(t))
                    byTitle[t] = ex;
            }
            var reqCount = doc.Requirements.Count;
            for (var i = 0; i < doc.Requirements.Count; i++)
            {
                var r = doc.Requirements[i];
                var title = r.Title?.Trim();
                if (string.IsNullOrWhiteSpace(title))
                {
                    errors.Add("Skipped requirement with empty title.");
                    continue;
                }
                try
                {
                    Requirement? existing = null;
                    string displayId;
                    if (!string.IsNullOrWhiteSpace(r.DisplayId))
                    {
                        displayId = r.DisplayId!.Trim();
                        byDisplayId.TryGetValue(displayId, out existing);
                    }
                    else
                    {
                        if (byTitle.TryGetValue(title, out existing))
                        {
                            displayId = existing.DisplayId;
                        }
                        else
                        {
                            displayId = await _slugService.AllocateSlugAsync(SlugEntityType.Requirement, project.DisplayId, cancellationToken);
                        }
                    }
                    var description = r.Description?.Trim();
                    Guid? keywordId = null;
                    if (!string.IsNullOrWhiteSpace(r.CategoryKeywordName))
                    {
                        var categoryName = r.CategoryKeywordName.Trim();
                        if (keywordIdByCategoryName.TryGetValue(categoryName, out var kid))
                        {
                            keywordId = kid;
                        }
                    }
                    var isUpdate = existing is not null;
                    var requirement = new Requirement
                    {
                        Id = isUpdate ? existing!.Id : Guid.NewGuid(),
                        DisplayId = displayId,
                        ProjectId = projectId,
                        ParentId = null,
                        KeywordId = keywordId,
                        Title = title,
                        Description = string.IsNullOrEmpty(description) ? null : description,
                        State = (RequirementState)(r.State ?? 0),
                        CreatedAt = isUpdate ? existing!.CreatedAt : now,
                        UpdatedAt = now
                    };
                    await _requirements.UpsertAsync(scope, requirement, cancellationToken);
                    if (!isUpdate)
                    {
                        requirementsCreated++;
                        byDisplayId[displayId] = requirement;
                        byTitle[title] = requirement;
                    }
                    if ((i + 1) % 10 == 0 || i == reqCount - 1)
                        _logger.LogInformation("Requirement ingest progress: {Current}/{Total} requirements, {Created} created", i + 1, reqCount, requirementsCreated);
                }
                catch (Exception ex)
                {
                    errors.Add($"{title}: {ex.Message}");
                    _logger.LogWarning(ex, "Requirement ingest: requirement failed. Title={Title}", title);
                }
            }
            _logger.LogInformation("Requirement ingest: requirements phase done. Created={Created}, Errors={ErrorCount}", requirementsCreated, errors.Count);
        }

        _logger.LogInformation("Requirement ingest completed. KeywordsCreated={KeywordsCreated}, RequirementsCreated={RequirementsCreated}, TotalErrors={ErrorCount}", keywordsCreated, requirementsCreated, errors.Count);
        return new IngestResult(requirementsCreated, errors, keywordsCreated);
    }

    private static string slugFromName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return "KW-unknown";
        var s = new string(name.Trim().Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '.' || c == '-').ToArray());
        s = string.Join("-", s.Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant();
        return "KW-" + (s.Length > 40 ? s[..40] : s);
    }

    private sealed class RequirementIngestItem
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
    }

    private sealed class RequirementIngestDocument
    {
        public List<KeywordIngestItem>? Keywords { get; set; }
        public List<RequirementDocumentItem>? Requirements { get; set; }
    }

    private sealed class KeywordIngestItem
    {
        public string? DisplayId { get; set; }
        public string? Name { get; set; }
    }

    private sealed class RequirementDocumentItem
    {
        public string? DisplayId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public int? State { get; set; }
        public Guid? ParentId { get; set; }
        public string? CategoryKeywordName { get; set; }
    }
}
