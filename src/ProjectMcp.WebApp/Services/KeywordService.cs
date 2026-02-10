using ProjectMcp.WebApp.Models;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Services;

public sealed class KeywordService : IKeywordService
{
    private readonly IKeywordRepository _keywords;

    public KeywordService(IKeywordRepository keywords)
    {
        _keywords = keywords;
    }

    public Task<IReadOnlyList<Keyword>> ListAsync(UserScope scope, Guid enterpriseId, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, enterpriseId);
        return _keywords.ListByEnterpriseAsync(enterpriseId, cancellationToken);
    }

    public async Task<Keyword?> GetAsync(UserScope scope, Guid id, CancellationToken cancellationToken = default)
    {
        var keyword = await _keywords.GetByIdAsync(id, cancellationToken);
        if (keyword is null)
        {
            return null;
        }

        ScopeValidation.EnsureEnterprise(scope, keyword.EnterpriseId);
        return keyword;
    }

    public async Task<Keyword> UpsertAsync(UserScope scope, Keyword keyword, CancellationToken cancellationToken = default)
    {
        ScopeValidation.EnsureEnterprise(scope, keyword.EnterpriseId);
        var existing = await _keywords.GetByIdAsync(keyword.Id, cancellationToken);
        return existing is null
            ? await _keywords.AddAsync(keyword, cancellationToken)
            : await _keywords.UpdateAsync(keyword, cancellationToken);
    }
}
