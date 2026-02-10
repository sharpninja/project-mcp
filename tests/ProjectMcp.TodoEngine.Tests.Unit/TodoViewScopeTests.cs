using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Exceptions;
using ProjectMCP.TodoEngine.Models;
using ProjectMCP.TodoEngine.Views;

namespace ProjectMCP.TodoEngine.Tests.Unit;

public sealed class TodoViewScopeTests
{
    [Fact]
    public async Task GetProjectAsync_ThrowsWhenEnterpriseMismatch()
    {
        var projectId = Guid.NewGuid();
        var project = new Project
        {
            Id = projectId,
            EnterpriseId = Guid.NewGuid(),
            DisplayId = "P001",
            Name = "Alpha"
        };
        var view = CreateView(project, null);
        var scope = new ScopeContext(Guid.NewGuid(), projectId);

        await Assert.ThrowsAsync<ScopeViolationException>(() => view.GetProjectAsync(scope));
    }

    [Fact]
    public async Task CreateWorkItemAsync_ThrowsWhenMissingProjectScope()
    {
        var view = CreateView(null, null);
        var scope = new ScopeContext(Guid.NewGuid(), null);
        var item = new WorkItem
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DisplayId = "WI000001",
            Title = "Task",
            Level = WorkItemLevel.Task
        };

        await Assert.ThrowsAsync<ScopeViolationException>(() => view.CreateWorkItemAsync(scope, item, null));
    }

    [Fact]
    public async Task UpdateWorkItemAsync_ThrowsWhenProjectMismatch()
    {
        var view = CreateView(null, null);
        var scope = new ScopeContext(Guid.NewGuid(), Guid.NewGuid());
        var item = new WorkItem
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DisplayId = "WI000002",
            Title = "Task",
            Level = WorkItemLevel.Task
        };

        await Assert.ThrowsAsync<ScopeViolationException>(() => view.UpdateWorkItemAsync(scope, item, null));
    }

    [Fact]
    public async Task DeleteWorkItemAsync_ThrowsWhenProjectMismatch()
    {
        var projectId = Guid.NewGuid();
        var item = new WorkItem
        {
            Id = Guid.NewGuid(),
            ProjectId = Guid.NewGuid(),
            DisplayId = "WI000003",
            Title = "Task",
            Level = WorkItemLevel.Task
        };
        var view = CreateView(null, item);
        var scope = new ScopeContext(Guid.NewGuid(), projectId);

        await Assert.ThrowsAsync<ScopeViolationException>(() => view.DeleteWorkItemAsync(scope, item.Id, null));
    }

    [Fact]
    public async Task ListWorkItemsAsync_ThrowsWhenMissingProjectScope()
    {
        var view = CreateView(null, null);
        var scope = new ScopeContext(Guid.NewGuid(), null);
        var filter = new WorkItemFilter();

        await Assert.ThrowsAsync<ScopeViolationException>(() => view.ListWorkItemsAsync(scope, filter));
    }

    private static TodoView CreateView(Project? project, WorkItem? item)
    {
        return new TodoView(
            new StubProjectRepository(project),
            new StubWorkItemRepository(item),
            new StubMilestoneRepository(),
            new StubReleaseRepository());
    }

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly Project? _project;

        public StubProjectRepository(Project? project)
        {
            _project = project;
        }

        public Task<Project?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_project);
        }

        public Task<Project?> GetBySlugAsync(string slug, Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Project?>(null);
        }

        public Task<IReadOnlyList<Project>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Project>>(Array.Empty<Project>());
        }

        public Task<Project> AddAsync(Project project, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Project> UpdateAsync(Project project, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }
    }

    private sealed class StubWorkItemRepository : IWorkItemRepository
    {
        private readonly WorkItem? _item;

        public StubWorkItemRepository(WorkItem? item)
        {
            _item = item;
        }

        public Task<WorkItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_item);
        }

        public Task<WorkItem?> GetBySlugAsync(string slug, Guid projectId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<WorkItem?>(null);
        }

        public Task<IReadOnlyList<WorkItem>> ListAsync(WorkItemFilter filter, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<WorkItem>>(Array.Empty<WorkItem>());
        }

        public Task<WorkItem> AddAsync(WorkItem item, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(item);
        }

        public Task<WorkItem> UpdateAsync(WorkItem item, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(item);
        }

        public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class StubMilestoneRepository : IMilestoneRepository
    {
        public Task<Milestone?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Milestone?>(null);
        }

        public Task<IReadOnlyList<Milestone>> ListByEnterpriseAsync(Guid enterpriseId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Milestone>>(Array.Empty<Milestone>());
        }

        public Task<Milestone> AddAsync(Milestone milestone, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Milestone> UpdateAsync(Milestone milestone, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubReleaseRepository : IReleaseRepository
    {
        public Task<Release?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Release?>(null);
        }

        public Task<IReadOnlyList<Release>> ListByProjectAsync(Guid projectId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Release>>(Array.Empty<Release>());
        }

        public Task<Release> AddAsync(Release release, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<Release> UpdateAsync(Release release, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
