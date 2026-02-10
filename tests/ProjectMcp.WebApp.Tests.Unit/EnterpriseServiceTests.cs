using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectMcp.WebApp.Models;
using ProjectMcp.WebApp.Services;
using ProjectMCP.TodoEngine.Data;
using ProjectMCP.TodoEngine.Exceptions;
using ProjectMCP.TodoEngine.Models;

namespace ProjectMcp.WebApp.Tests.Unit;

public sealed class EnterpriseServiceTests
{
    [Fact]
    public async Task ListAsync_ReturnsOnlyAllowedEnterprises()
    {
        using var db = SqliteTestDatabase.Create();
        var now = DateTimeOffset.UtcNow;
        var enterpriseA = new Enterprise { Id = Guid.NewGuid(), DisplayId = "ENT001", Name = "A", CreatedAt = now, UpdatedAt = now };
        var enterpriseB = new Enterprise { Id = Guid.NewGuid(), DisplayId = "ENT002", Name = "B", CreatedAt = now, UpdatedAt = now };
        db.Context.Enterprises.AddRange(enterpriseA, enterpriseB);
        await db.Context.SaveChangesAsync();
        var service = new EnterpriseService(db.Context);
        var scope = new UserScope(new[] { enterpriseA.Id }, Array.Empty<Guid>(), null);

        var results = await service.ListAsync(scope);

        Assert.Single(results);
        Assert.Equal(enterpriseA.Id, results[0].Id);
    }

    [Fact]
    public async Task GetAsync_ThrowsWhenOutOfScope()
    {
        using var db = SqliteTestDatabase.Create();
        var enterpriseId = Guid.NewGuid();
        var service = new EnterpriseService(db.Context);
        var scope = new UserScope(Array.Empty<Guid>(), Array.Empty<Guid>(), null);

        await Assert.ThrowsAsync<ScopeViolationException>(() => service.GetAsync(scope, enterpriseId));
    }

    private sealed class SqliteTestDatabase : IDisposable
    {
        public SqliteConnection Connection { get; }
        public TodoEngineDbContext Context { get; }

        private SqliteTestDatabase(SqliteConnection connection, TodoEngineDbContext context)
        {
            Connection = connection;
            Context = context;
        }

        public static SqliteTestDatabase Create()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            var options = new DbContextOptionsBuilder<TodoEngineDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new TodoEngineDbContext(options);
            context.Database.EnsureCreated();
            return new SqliteTestDatabase(connection, context);
        }

        public void Dispose()
        {
            Context.Dispose();
            Connection.Dispose();
        }
    }
}
