using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Abstractions;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Models;
using ProjectMCP.TodoEngine.Services;

namespace ProjectMCP.TodoEngine.Tests.Unit;

public sealed class SlugServiceTests
{
    [Fact]
    public async Task AllocateSlugAsync_IncrementsForExistingSlugs()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var now = DateTimeOffset.UtcNow;
        var enterpriseId = Guid.NewGuid();

        context.Enterprises.Add(new Enterprise
        {
            Id = enterpriseId,
            DisplayId = "ENT001",
            Name = "Enterprise",
            CreatedAt = now,
            UpdatedAt = now
        });

        context.Projects.AddRange(
            new Project
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DisplayId = "P001",
                Name = "Alpha"
            },
            new Project
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DisplayId = "P002",
                Name = "Beta"
            });
        await context.SaveChangesAsync();

        var service = new SlugService(context);

        var slug = await service.AllocateSlugAsync(SlugEntityType.Project, string.Empty);

        Assert.Equal("P003", slug);
    }

    [Fact]
    public async Task AllocateSlugAsync_UsesOwnerPrefix()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var now = DateTimeOffset.UtcNow;
        var enterpriseId = Guid.NewGuid();

        context.Enterprises.Add(new Enterprise
        {
            Id = enterpriseId,
            DisplayId = "ENT002",
            Name = "Enterprise",
            CreatedAt = now,
            UpdatedAt = now
        });

        context.Projects.AddRange(
            new Project
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DisplayId = "ACME-P001",
                Name = "Alpha"
            },
            new Project
            {
                Id = Guid.NewGuid(),
                EnterpriseId = enterpriseId,
                DisplayId = "ACME-P010",
                Name = "Beta"
            });
        await context.SaveChangesAsync();

        var service = new SlugService(context);

        var slug = await service.AllocateSlugAsync(SlugEntityType.Project, "ACME");

        Assert.Equal("ACME-P011", slug);
    }

    [Fact]
    public async Task AllocateSlugAsync_ThrowsForUnknownEntityType()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        var service = new SlugService(context);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.AllocateSlugAsync((SlugEntityType)999, "ACME"));
    }

    private static TodoEngineDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<TodoEngineDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new TodoEngineDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
