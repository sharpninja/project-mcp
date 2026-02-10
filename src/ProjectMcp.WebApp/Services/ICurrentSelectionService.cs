namespace ProjectMcp.WebApp.Services;

/// <summary>Current enterprise and project selection shown in the top banner.</summary>
public interface ICurrentSelectionService
{
    Guid? EnterpriseId { get; }
    string? EnterpriseName { get; }
    Guid? ProjectId { get; }
    string? ProjectName { get; }

    void SetSelection(Guid? enterpriseId, string? enterpriseName, Guid? projectId, string? projectName);

    /// <summary>Fired when selection changes so UI can refresh.</summary>
    event Action? SelectionChanged;
}
