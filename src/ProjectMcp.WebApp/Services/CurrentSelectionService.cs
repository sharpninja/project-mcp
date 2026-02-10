namespace ProjectMcp.WebApp.Services;

public sealed class CurrentSelectionService : ICurrentSelectionService
{
    public Guid? EnterpriseId { get; private set; }
    public string? EnterpriseName { get; private set; }
    public Guid? ProjectId { get; private set; }
    public string? ProjectName { get; private set; }

    public event Action? SelectionChanged;

    public void SetSelection(Guid? enterpriseId, string? enterpriseName, Guid? projectId, string? projectName)
    {
        EnterpriseId = enterpriseId;
        EnterpriseName = enterpriseName;
        ProjectId = projectId;
        ProjectName = projectName;
        SelectionChanged?.Invoke();
    }
}
