using ProjectMcp.WebApp.Models;
using ProjectMcp.WebApp.Services;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Exceptions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Tests.Unit;

public sealed class ProjectServiceTests
{
    [Fact]
    public async Task ListAsync_FiltersByAllowedProjects()
    {
        var enterpriseId = Guid.NewGuid();
        var projectA = new Project { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DisplayId = "P001", Name = "A" };
        var projectB = new Project { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DisplayId = "P002", Name = "B" };
        var repository = new StubProjectRepository(new[] { projectA, projectB });
        var service = new ProjectService(repository);
        var scope = new UserScope(new[] { enterpriseId }, new[] { projectA.Id }, null);

        var results = await service.ListAsync(scope);

        Assert.Single(results);
        Assert.Equal(projectA.Id, results[0].Id);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenEnterpriseOutOfScope()
    {
        var enterpriseId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DisplayId = "P001", Name = "A" };
        var repository = new StubProjectRepository(new[] { project });
        var service = new ProjectService(repository);
        var scope = new UserScope(Array.Empty<Guid>(), Array.Empty<Guid>(), null);

        await Assert.ThrowsAsync<ScopeViolationException>(() => service.GetAsync(scope, project.Id));
    }

    [Fact]
    public async Task UpsertAsync_AddsWhenMissing()
    {
        var enterpriseId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), EnterpriseId = enterpriseId, DisplayId = "P001", Name = "A" };
        var repository = new StubProjectRepository(Array.Empty<Project>());
        var service = new ProjectService(repository);
        var scope = new UserScope(new[] { enterpriseId }, Array.Empty<Guid>(), null);

        var result = await service.UpsertAsync(scope, project);

        Assert.Equal(project.Id, result.Id);
        Assert.True(repository.AddCalled);
        Assert.False(repository.UpdateCalled);
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly List<Project> _projects;

        public StubProjectRepository(IEnumerable<Project> projects)
        {
            _projects = projects.ToList();
        }

        public bool AddCalled { get; private set; }
        public bool UpdateCalled { get; private set; }

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_projects.FirstOrDefault(p => p.Id == id));
        }

        public Task<Project?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_projects.FirstOrDefault(p => p.DisplayId == slug && p.EnterpriseId == enterpriseId));
        }

        public Task<IReadOnlyList<Project>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Project> results = _projects.Where(p => p.EnterpriseId == enterpriseId).ToList();
            return Task.FromResult(results);
        }

        public Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            AddCalled = true;
            _projects.Add(project);
            return Task.FromResult(project);
        }

        public Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            UpdateCalled = true;
            return Task.FromResult(project);
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_projects.Any(p => p.Id == id));
        }
    }
}
