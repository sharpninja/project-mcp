using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class SystemService : ISystemService
{
    private readonly ISystemRepository _systems;

    public SystemService(ISystemRepository systems)
    {
        _systems = systems;
    }

    public Task<IReadOnlyList<SystemEntity>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _systems.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<SystemEntity?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var system = await _systems.GetByIdAsync(id, cancellationToken);
        if (system is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, system.EnterpriseId);
        return system;
    }

    public async Task<SystemEntity> UpsertAsync(UserScope scope, SystemEntity system, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, system.EnterpriseId);
        return system.Id == Guid.Empty
            ? await _systems.AddAsync(system, cancellationToken)
            : await _systems.UpdateAsync(system, cancellationToken);
    }
}
