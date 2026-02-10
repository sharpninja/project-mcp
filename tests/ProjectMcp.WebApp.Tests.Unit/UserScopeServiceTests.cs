using System.Security.Claims;
using Microsoft.Extensions.Options;
using ProjectMcp.WebApp.Services;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Tests.Unit;

public sealed class UserScopeServiceTests
{
    [Fact]
    public async Task GetScopeAsync_ReturnsScopeFromDbWhenNameIdentifierMatches()
    {
        var enterpriseId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, "agent-123") }, "test"));

        var resourceRepo = new StubResourceRepository(
            resolveByOAuth2Sub: subject => subject == "agent-123" ? new Resource { Id = resourceId } : null,
            getById: _ => null);
        var projectResourceRepo = new StubProjectResourceRepository(resourceId, enterpriseId, projectId);
        var options = Options.Create(new ScopeOptions());

        var service = new UserScopeService(resourceRepo, projectResourceRepo, options);

        var scope = await service.GetScopeAsync(principal);

        Assert.Equal(new[] { enterpriseId }, scope.AllowedEnterpriseIds);
        Assert.Equal(new[] { projectId }, scope.AllowedProjectIds);
        Assert.Equal(resourceId, scope.CurrentResourceId);
    }

    [Fact]
    public async Task GetScopeAsync_ReturnsEmptyWhenNoResourceResolved()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test"));
        var resourceRepo = new StubResourceRepository(resolveByOAuth2Sub: _ => null, getById: _ => null);
        var projectResourceRepo = new StubProjectResourceRepository(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var options = Options.Create(new ScopeOptions());

        var service = new UserScopeService(resourceRepo, projectResourceRepo, options);

        var scope = await service.GetScopeAsync(principal);

        Assert.Empty(scope.AllowedEnterpriseIds);
        Assert.Empty(scope.AllowedProjectIds);
        Assert.Null(scope.CurrentResourceId);
    }

    [Fact]
    public async Task GetScopeAsync_UsesDefaultOAuth2SubWhenConfigured()
    {
        var enterpriseId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test"));

        var resourceRepo = new StubResourceRepository(
            resolveByOAuth2Sub: subject => subject == "sharpninja" ? new Resource { Id = resourceId } : null,
            getById: _ => null);
        var projectResourceRepo = new StubProjectResourceRepository(resourceId, enterpriseId, projectId);
        var options = Options.Create(new ScopeOptions { DefaultOAuth2Sub = "sharpninja" });

        var service = new UserScopeService(resourceRepo, projectResourceRepo, options);

        var scope = await service.GetScopeAsync(principal);

        Assert.Equal(new[] { enterpriseId }, scope.AllowedEnterpriseIds);
        Assert.Equal(new[] { projectId }, scope.AllowedProjectIds);
        Assert.Equal(resourceId, scope.CurrentResourceId);
    }

    [Fact]
    public async Task GetScopeAsync_UsesDefaultResourceIdWhenConfigured()
    {
        var enterpriseId = Guid.NewGuid();
        var projectId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(Array.Empty<Claim>(), "test"));

        var resourceRepo = new StubResourceRepository(
            resolveByOAuth2Sub: _ => null,
            getById: id => id == resourceId ? new Resource { Id = resourceId } : null);
        var projectResourceRepo = new StubProjectResourceRepository(resourceId, enterpriseId, projectId);
        var options = Options.Create(new ScopeOptions { DefaultResourceId = resourceId });

        var service = new UserScopeService(resourceRepo, projectResourceRepo, options);

        var scope = await service.GetScopeAsync(principal);

        Assert.Equal(new[] { enterpriseId }, scope.AllowedEnterpriseIds);
        Assert.Equal(new[] { projectId }, scope.AllowedProjectIds);
        Assert.Equal(resourceId, scope.CurrentResourceId);
    }

    private sealed class StubResourceRepository : IResourceRepository
    {
        private readonly Func<string, Resource?> _resolveByOAuth2Sub;
        private readonly Func<Guid, Resource?> _getById;

        public StubResourceRepository(Func<string, Resource?> resolveByOAuth2Sub, Func<Guid, Resource?> getById)
        {
            _resolveByOAuth2Sub = resolveByOAuth2Sub;
            _getById = getById;
        }

        public Task<Resource?> ResolveByOAuth2SubAsync(string oauth2Sub, CancellationToken cancellationToken = default)
            => Task.FromResult(_resolveByOAuth2Sub(oauth2Sub));

        public Task<Resource?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(_getById(id));

        public Task<Resource?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Resource?> ResolveAgentNameToResourceAsync(string agentName, Guid enterpriseId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<Resource>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }

    private sealed class StubProjectResourceRepository : IProjectResourceRepository
    {
        private readonly Guid _resourceId;
        private readonly Guid _enterpriseId;
        private readonly Guid _projectId;

        public StubProjectResourceRepository(Guid resourceId, Guid enterpriseId, Guid projectId)
        {
            _resourceId = resourceId;
            _enterpriseId = enterpriseId;
            _projectId = projectId;
        }

        public Task<(IReadOnlyList<Guid> EnterpriseIds, IReadOnlyList<Guid> ProjectIds)> GetScopeForResourceAsync(Guid resourceId, CancellationToken cancellationToken = default)
        {
            if (resourceId != _resourceId)
                return Task.FromResult<(IReadOnlyList<Guid>, IReadOnlyList<Guid>)>((Array.Empty<Guid>(), Array.Empty<Guid>()));
            return Task.FromResult<(IReadOnlyList<Guid>, IReadOnlyList<Guid>)>(
                (new[] { _enterpriseId }, new[] { _projectId }));
        }
    }
}
