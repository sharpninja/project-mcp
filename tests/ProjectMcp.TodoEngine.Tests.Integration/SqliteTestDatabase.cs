using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectMCP.TodoEngine.Data;

namespace ProjectMCP.TodoEngine.Tests.Integration;

internal sealed class SqliteTestDatabase : IDisposable
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
