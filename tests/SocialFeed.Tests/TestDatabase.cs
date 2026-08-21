using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SocialFeed.Data;

namespace SocialFeed.Tests;

/// <summary>
/// An empty database for one test, held in memory.
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
