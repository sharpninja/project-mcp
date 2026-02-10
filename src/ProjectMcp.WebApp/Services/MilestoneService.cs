using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class MilestoneService : IMilestoneService
{
    private readonly IMilestoneRepository _milestones;

    public MilestoneService(IMilestoneRepository milestones)
    {
        _milestones = milestones;
    }

    public Task<IReadOnlyList<Milestone>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _milestones.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<Milestone?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var milestone = await _milestones.GetByIdAsync(id, cancellationToken);
        if (milestone is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, milestone.EnterpriseId);
        return milestone;
    }

    public async Task<Milestone> UpsertAsync(UserScope scope, Milestone milestone, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, milestone.EnterpriseId);
        return milestone.Id == Guid.Empty
            ? await _milestones.AddAsync(milestone, cancellationToken)
            : await _milestones.UpdateAsync(milestone, cancellationToken);
    }
}
