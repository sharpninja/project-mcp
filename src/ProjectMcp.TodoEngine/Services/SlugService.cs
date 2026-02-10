using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using Serilog;

namespace ProjectMCP.TodoEngine.Services;

public sealed class SlugService : ISlugService
{
    private static readonly ILogger Log = Serilog.Log.ForContext<SlugService>();
    private readonly TodoEngineDbContext _dbContext;

    private static readonly IReadOnlyDictionary<SlugEntityType, (string Prefix, int Width)> Formats =
        new Dictionary<SlugEntityType, (string Prefix, int Width)>
        {
            [SlugEntityType.Project] = ("P", 3),
            [SlugEntityType.WorkItem] = ("WI", 6),
            [SlugEntityType.Milestone] = ("MS", 4),
            [SlugEntityType.Release] = ("REL", 4),
            [SlugEntityType.Keyword] = ("KW", 4),
            [SlugEntityType.Issue] = ("ISS", 4),
            [SlugEntityType.Standard] = ("STD", 4),
            [SlugEntityType.Requirement] = ("REQ", 4)
        };

    public SlugService(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<string> AllocateSlugAsync(SlugEntityType entityType, string ownerSlug, CancellationToken cancellationToken = default)
    {
        if (!Formats.TryGetValue(entityType, out var format))
        {
            Log.Warning("Slug allocation requested for unsupported entity type {EntityType}", entityType);
            throw new ArgumentOutOfRangeException(nameof(entityType), entityType, "Unsupported entity type.");
        }

        var normalizedOwner = ownerSlug?.Trim() ?? string.Empty;
        var prefix = format.Prefix;
        var width = format.Width;
        var baseSlug = string.IsNullOrWhiteSpace(normalizedOwner)
            ? prefix
            : $"{normalizedOwner}-{prefix}";

        var displayIds = await GetDisplayIds(entityType)
            .Where(slug => slug.StartsWith(baseSlug))
            .ToListAsync(cancellationToken);

        var nextIndex = displayIds
            .Select(ParseTrailingIndex)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var slug = $"{baseSlug}{nextIndex.ToString($"D{width}")}";
        Log.Debug("Allocated slug {Slug} for {EntityType} owner {OwnerSlug}", slug, entityType, normalizedOwner);
        return slug;
    }

    private IQueryable<string> GetDisplayIds(SlugEntityType entityType)
    {
        return entityType switch
        {
            SlugEntityType.Project => _dbContext.Projects.Select(p => p.DisplayId),
            SlugEntityType.WorkItem => _dbContext.WorkItems.Select(w => w.DisplayId),
            SlugEntityType.Milestone => _dbContext.Milestones.Select(m => m.DisplayId),
            SlugEntityType.Release => _dbContext.Releases.Select(r => r.DisplayId),
            SlugEntityType.Requirement => _dbContext.Requirements.Select(r => r.DisplayId),
            SlugEntityType.Standard => _dbContext.Standards.Select(s => s.DisplayId),
            SlugEntityType.Issue => _dbContext.Issues.Select(i => i.DisplayId),
            SlugEntityType.Keyword => _dbContext.Keywords.Select(k => k.DisplayId),
            _ => throw new NotSupportedException($"Slug allocation for {entityType} is not wired in the DbContext yet.")
        };
    }

    private static int ParseTrailingIndex(string slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return 0;
        }

        var digits = new string(slug.Reverse().TakeWhile(char.IsDigit).Reverse().ToArray());
        return int.TryParse(digits, out var value) ? value : 0;
    }
}
