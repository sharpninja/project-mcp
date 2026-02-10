using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;
using ProjectMCP.TodoEngine.Repositories;
using ProjectMCP.TodoEngine.Views;

namespace ProjectMCP.TodoEngine.Tests.Integration;

public sealed class TodoViewIntegrationTests
{
    [Fact]
    public async Task UpsertProjectAsync_CreatesProjectWithinScope()
    {
        using var db = SqliteTestDatabase.Create();
        var view = new TodoView(
            new ProjectRepository(db.Context),
            new WorkItemRepository(db.Context),
            new MilestoneRepository(db.Context),
            new ReleaseRepository(db.Context));
        var now = DateTimeOffset.UtcNow;
        var enterpriseId = Guid.NewGuid();
        var scope = new ScopeContext(enterpriseId, null);
        var project = new Project
        {
            DisplayId = "P001",
            Name = "Alpha"
        };

        db.Context.Enterprises.Add(new Enterprise
        {
            Id = enterpriseId,
            DisplayId = "ENT001",
            Name = "Enterprise",
            CreatedAt = now,
            UpdatedAt = now
        });
        await db.Context.SaveChangesAsync();

        var saved = await view.UpsertProjectAsync(scope, project, null);

        Assert.Equal(enterpriseId, saved.EnterpriseId);
        Assert.NotEqual(Guid.Empty, saved.Id);
        Assert.NotEqual(default, saved.CreatedAt);
        Assert.True(saved.UpdatedAt >= saved.CreatedAt);

        var stored = await db.Context.Projects.FirstOrDefaultAsync(p => p.Id == saved.Id);
        Assert.NotNull(stored);
    }
}
