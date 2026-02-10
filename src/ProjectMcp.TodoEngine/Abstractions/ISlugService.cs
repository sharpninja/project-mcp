namespace ProjectMCP.TodoEngine.Abstractions;

public interface ISlugService
{
    Task<string> AllocateSlugAsync(SlugEntityType entityType, string ownerSlug, CancellationToken cancellationToken = default);
}
