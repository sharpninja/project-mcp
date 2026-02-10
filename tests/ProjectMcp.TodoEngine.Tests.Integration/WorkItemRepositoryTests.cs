using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Models;
using ProjectMCP.TodoEngine.Repositories;

namespace ProjectMCP.TodoEngine.Tests.Integration;

public sealed class WorkItemRepositoryTests
{
    [Fact]
    public async Task ListAsync_FiltersByProjectAndStatus()
    {
        using var db = SqliteTestDatabase.Create();
        var now = DateTimeOffset.UtcNow;
        var enterprise = new Enterprise
        {
            Id = Guid.NewGuid(),
            DisplayId = "ENT001",
            Name = "Enterprise",
            CreatedAt = now,
            UpdatedAt = now
        };
        var projectA = new Project
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterprise.Id,
            DisplayId = "P001",
            Name = "Project A",
            CreatedAt = now,
            UpdatedAt = now
        };
        var projectB = new Project
        {
            Id = Guid.NewGuid(),
            EnterpriseId = enterprise.Id,
            DisplayId = "P002",
            Name = "Project B",
            CreatedAt = now,
            UpdatedAt = now
        };

        db.Context.Enterprises.Add(enterprise);
        db.Context.Projects.AddRange(projectA, projectB);
        db.Context.WorkItems.AddRange(
            new WorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectA.Id,
                DisplayId = "WI000001",
                Title = "Todo",
                Level = WorkItemLevel.Task,
                Status = WorkItemStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now
            },
            new WorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectB.Id,
                DisplayId = "WI000002",
                Title = "Other",
                Level = WorkItemLevel.Task,
                Status = WorkItemStatus.Todo,
                CreatedAt = now,
                UpdatedAt = now
            },
            new WorkItem
            {
                Id = Guid.NewGuid(),
                ProjectId = projectA.Id,
                DisplayId = "WI000003",
                Title = "Done",
                Level = WorkItemLevel.Task,
                Status = WorkItemStatus.Done,
                CreatedAt = now,
                UpdatedAt = now
            });
        await db.Context.SaveChangesAsync();

        var repository = new WorkItemRepository(db.Context);

        var results = await repository.ListAsync(new WorkItemFilter
        {
            ProjectId = projectA.Id,
            Status = WorkItemStatus.Todo
        });

        Assert.Single(results);
        Assert.Equal(projectA.Id, results[0].ProjectId);
        Assert.Equal(WorkItemStatus.Todo, results[0].Status);
    }
}
