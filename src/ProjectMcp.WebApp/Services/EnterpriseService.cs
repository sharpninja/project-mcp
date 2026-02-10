using Microsoft.EntityFrameworkCore;
using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class EnterpriseService : IEnterpriseService
{
    private readonly TodoEngineDbContext _dbContext;

    public EnterpriseService(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Enterprise>> ListAsync(UserScope scope, CancellationToken cancellationToken = default)
    {
        if (scope.AllowedEnterpriseIds.Count == 0)
        {
            return Array.Empty<Enterprise>();
        }

        return await _dbContext.Enterprises
            .Where(e => scope.AllowedEnterpriseIds.Contains(e.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<Enterprise?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, id);
        return await _dbContext.Enterprises.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }
}
