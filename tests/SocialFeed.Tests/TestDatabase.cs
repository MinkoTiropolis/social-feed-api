using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;

namespace SocialFeed.Tests;

/// <summary>
/// An empty database for one test, held in memory.
/// <para>
/// SQLite rather than the in-memory provider because these tests exercise real SQL: the
/// purge uses ExecuteDelete, and the cascade that removes a post's likes is a foreign key
/// rule the database enforces. The in-memory provider supports neither.
/// </para>
/// </summary>
public sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestDatabase()
    {
        // The connection has to stay open: an in-memory SQLite database exists only as long
        // as something is connected to it.
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        Context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .Options);

        Context.Database.EnsureCreated();
    }

    public AppDbContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }
}
