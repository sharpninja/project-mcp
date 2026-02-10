namespace ProjectMCP.TodoEngine.Models;

public enum ProjectStatus
{
    Active,
    OnHold,
    Archived
}

public enum WorkItemLevel
{
    Work,
    Task
}

public enum WorkItemState
{
    Planned,
    Active,
    Blocked,
    Done,
    Cancelled
}

public enum WorkItemStatus
{
    Todo,
    InProgress,
    Done,
    Cancelled
}

public enum MilestoneState
{
    Planned,
    Active,
    Completed,
    Cancelled
}

public enum RequirementState
{
    Draft,
    Active,
    Done,
    Cancelled
}

public enum IssueState
{
    Open,
    InProgress,
    Done,
    Closed
}

public sealed class Enterprise
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class Project
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Active;
    public string? TechStackJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

/// <summary>Junction: which resources have access to which projects (for scope resolution).</summary>
public sealed class ProjectResource
{
    public Guid ProjectId { get; set; }
    public Guid ResourceId { get; set; }
    public Project? Project { get; set; }
    public Resource? Resource { get; set; }
}

public sealed class WorkItem
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public WorkItemLevel Level { get; set; }
    public WorkItemState? State { get; set; }
    public WorkItemStatus? Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? ResourceId { get; set; }
    public Guid? MilestoneId { get; set; }
    public Guid? ReleaseId { get; set; }
    public DateTimeOffset? StartDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public double? EffortHours { get; set; }
    public int? Complexity { get; set; }
    public int? Priority { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project? Project { get; set; }
    public WorkItem? Parent { get; set; }
}

public sealed class Milestone
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public MilestoneState State { get; set; } = MilestoneState.Planned;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class Release
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? TagVersion { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project? Project { get; set; }
}

public sealed class Resource
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OAuth2Sub { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class Requirement
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public Guid? ParentId { get; set; }
    public Guid? KeywordId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public RequirementState State { get; set; } = RequirementState.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project? Project { get; set; }
    public Keyword? Keyword { get; set; }
}

public sealed class Standard
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public Guid? ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class Issue
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid ProjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IssueState State { get; set; } = IssueState.Open;
    public Guid? ResourceId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Project? Project { get; set; }
}

public sealed class Keyword
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class Domain
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class SystemEntity
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}

public sealed class Asset
{
    public Guid Id { get; set; }
    public string DisplayId { get; set; } = string.Empty;
    public Guid EnterpriseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? AssetType { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Enterprise? Enterprise { get; set; }
}
