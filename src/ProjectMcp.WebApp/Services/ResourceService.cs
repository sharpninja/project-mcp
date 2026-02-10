using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class ResourceService : IResourceService
{
    private readonly IResourceRepository _resources;

    public ResourceService(IResourceRepository resources)
    {
        _resources = resources;
    }

    public Task<IReadOnlyList<Resource>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _resources.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<Resource?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var resource = await _resources.GetByIdAsync(id, cancellationToken);
        if (resource is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, resource.EnterpriseId);
        return resource;
    }
}
