using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMCP.TodoEngine.Repositories;

public sealed class KeywordRepository : IKeywordRepository
{
    private readonly TodoEngineDbContext _dbContext;

    public KeywordRepository(TodoEngineDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Keyword?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Keywords.FirstOrDefaultAsync(k => k.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Keyword>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Keywords
            .Where(k => k.EnterpriseId == enterpriseId)
            .ToListAsync(cancellationToken);
    }

    public async Task<Keyword> AddAsync(Keyword keyword, CancellationToken cancellationToken = default)
    {
        _dbContext.Keywords.Add(keyword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return keyword;
    }

    public async Task<Keyword> UpdateAsync(Keyword keyword, CancellationToken cancellationToken = default)
    {
        _dbContext.Keywords.Update(keyword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return keyword;
    }
}
