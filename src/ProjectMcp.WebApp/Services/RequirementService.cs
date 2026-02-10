using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class RequirementService : IRequirementService
{
    private readonly IRequirementRepository _requirements;

    public RequirementService(IRequirementRepository requirements)
    {
        _requirements = requirements;
    }

    public Task<IReadOnlyList<Requirement>> ListAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, projectId);
        return _requirements.ListByProjectAsync(projectId, cancellationToken);
    }

    public async Task<Requirement?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var requirement = await _requirements.GetByIdAsync(id, cancellationToken);
        if (requirement is null)
        {
            return null;
        }

        ScopeValidation.EnsureProject(scope, requirement.ProjectId);
        return requirement;
    }

    public async Task<Requirement> UpsertAsync(UserScope scope, Requirement requirement, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, requirement.ProjectId);
        var existing = await _requirements.GetByIdAsync(requirement.Id, cancellationToken);
        return existing is null
            ? await _requirements.AddAsync(requirement, cancellationToken)
            : await _requirements.UpdateAsync(requirement, cancellationToken);
    }
}
