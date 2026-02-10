using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectMCP.TodoEngine.Data;

public sealed class TodoEngineDbContextFactory : IDesignTimeDbContextFactory<TodoEngineDbContext>
{
    public TodoEngineDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("PROJECT_MCP_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("DATABASE_URL");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Set PROJECT_MCP_CONNECTION_STRING or DATABASE_URL to generate migrations.");
        }

        var options = new DbContextOptionsBuilder<TodoEngineDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new TodoEngineDbContext(options);
    }
}
