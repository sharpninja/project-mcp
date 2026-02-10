using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class DomainService : IDomainService
{
    private readonly IDomainRepository _domains;

    public DomainService(IDomainRepository domains)
    {
        _domains = domains;
    }

    public Task<IReadOnlyList<Domain>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _domains.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<Domain?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var domain = await _domains.GetByIdAsync(id, cancellationToken);
        if (domain is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, domain.EnterpriseId);
        return domain;
    }

    public async Task<Domain> UpsertAsync(UserScope scope, Domain domain, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, domain.EnterpriseId);
        return domain.Id == Guid.Empty
            ? await _domains.AddAsync(domain, cancellationToken)
            : await _domains.UpdateAsync(domain, cancellationToken);
    }
}
