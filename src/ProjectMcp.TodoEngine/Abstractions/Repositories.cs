using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Abstractions;

public sealed class WorkItemFilter
{
    public Guid? ProjectId { get; init; }
    public Guid? ParentId { get; init; }
    public WorkItemLevel? Level { get; init; }
    public WorkItemStatus? Status { get; init; }
    public Guid? MilestoneId { get; init; }
    public Guid? ResourceId { get; init; }
}

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Project?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Project>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default);
    Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IWorkItemRepository
{
    Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<WorkItem?> GetBySlugAsync(string slug, Guid projectId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkItem>> ListAsync(WorkItemFilter filter, CancellationToken cancellationToken = default);
    Task<WorkItem> AddAsync(WorkItem item, CancellationToken cancellationToken = default);
    Task<WorkItem> UpdateAsync(WorkItem item, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public interface IMilestoneRepository
{
    Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Milestone>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Milestone> AddAsync(Milestone milestone, CancellationToken cancellationToken = default);
    Task<Milestone> UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default);
}

public interface IReleaseRepository
{
    Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Release>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Release> AddAsync(Release release, CancellationToken cancellationToken = default);
    Task<Release> UpdateAsync(Release release, CancellationToken cancellationToken = default);
}

public interface IResourceRepository
{
    Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Resource?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Resource?> ResolveAgentNameToResourceAsync(string agentName, Guid enterpriseId, CancellationToken cancellationToken = default);
    /// <summary>Resolve a user to a resource by OAuth2 subject (e.g. GitHub user id). Returns the first matching resource if the user exists in multiple enterprises.</summary>
    Task<Resource?> ResolveByOAuth2SubAsync(string oauth2Sub, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Resource>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
}

/// <summary>Returns allowed enterprises and projects for a resource (from project_resources and projects).</summary>
public interface IProjectResourceRepository
{
    Task<(IReadOnlyList<Guid> EnterpriseIds, IReadOnlyList<Guid> ProjectIds)> GetScopeForResourceAsync(Guid resourceId, CancellationToken cancellationToken = default);
}

public interface IRequirementRepository
{
    Task<Requirement?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Requirement>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Requirement> AddAsync(Requirement requirement, CancellationToken cancellationToken = default);
    Task<Requirement> UpdateAsync(Requirement requirement, CancellationToken cancellationToken = default);
}

public interface IStandardRepository
{
    Task<Standard?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Standard>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Standard>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Standard> AddAsync(Standard standard, CancellationToken cancellationToken = default);
    Task<Standard> UpdateAsync(Standard standard, CancellationToken cancellationToken = default);
}

public interface IIssueRepository
{
    Task<Issue?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Issue>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default);
    Task<Issue> AddAsync(Issue issue, CancellationToken cancellationToken = default);
    Task<Issue> UpdateAsync(Issue issue, CancellationToken cancellationToken = default);
}

public interface IKeywordRepository
{
    Task<Keyword?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Keyword>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Keyword> AddAsync(Keyword keyword, CancellationToken cancellationToken = default);
    Task<Keyword> UpdateAsync(Keyword keyword, CancellationToken cancellationToken = default);
}

public interface IDomainRepository
{
    Task<Domain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Domain>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Domain> AddAsync(Domain domain, CancellationToken cancellationToken = default);
    Task<Domain> UpdateAsync(Domain domain, CancellationToken cancellationToken = default);
}

public interface ISystemRepository
{
    Task<SystemEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemEntity>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<SystemEntity> AddAsync(SystemEntity system, CancellationToken cancellationToken = default);
    Task<SystemEntity> UpdateAsync(SystemEntity system, CancellationToken cancellationToken = default);
}

public interface IAssetRepository
{
    Task<Asset?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Asset>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default);
    Task<Asset> AddAsync(Asset asset, CancellationToken cancellationToken = default);
    Task<Asset> UpdateAsync(Asset asset, CancellationToken cancellationToken = default);
}
