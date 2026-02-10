namespace ProjectMcp.WebApp.Models;

public sealed record GanttItem(Guid Id, string Title, DateTimeOffset? Start, DateTimeOffset? Due);
