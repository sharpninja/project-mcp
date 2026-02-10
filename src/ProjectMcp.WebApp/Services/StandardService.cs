using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class StandardService : IStandardService
{
    private readonly IStandardRepository _standards;

    public StandardService(IStandardRepository standards)
    {
        _standards = standards;
    }

    public Task<IReadOnlyList<Standard>> ListEnterpriseAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _standards.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public Task<IReadOnlyList<Standard>> ListProjectAsync(UserScope scope, Guid projectId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureProject(scope, projectId);
        return _standards.ListByProjectAsync(projectId, cancellationToken);
    }

    public async Task<Standard?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var standard = await _standards.GetByIdAsync(id, cancellationToken);
        if (standard is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, standard.EnterpriseId);
        if (standard.ProjectId.HasValue)
        {
            ScopeValidation.EnsureProject(scope, standard.ProjectId.Value);
        }

        return standard;
    }

    public async Task<Standard> UpsertAsync(UserScope scope, Standard standard, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, standard.EnterpriseId);
        if (standard.ProjectId.HasValue)
        {
            ScopeValidation.EnsureProject(scope, standard.ProjectId.Value);
        }

        return standard.Id == Guid.Empty
            ? await _standards.AddAsync(standard, cancellationToken)
            : await _standards.UpdateAsync(standard, cancellationToken);
    }
}
