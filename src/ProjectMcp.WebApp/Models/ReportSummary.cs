namespace ProjectMcp.WebApp.Models;

public sealed record ReportSummary(Guid ProjectId, int TotalTasks, int DoneTasks, int InProgressTasks);
