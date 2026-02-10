using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class DomainRepository : IDomainRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public DomainRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Domain?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Domains.FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Domains
            .Where(d => d.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Domain> AddAsync(Domain domain, CancellationToken cancellationToken = default)
    {
        _dbContext.Domains.Add(domain);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return domain;
    }

    public async Task<Domain> UpdateAsync(Domain domain, CancellationToken cancellationToken = default)
    {
        _dbContext.Domains.Update(domain);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return domain;
    }
}
