using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SocialFeed.Data;

namespace SocialFeed.Tests;

/// <summary>
/// Boots the real API for integration tests, pointed at its own database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=localhost,1433;Database=SocialFeed_IntegrationTests;User Id=sa;Password=LocalDev_Passw0rd!;TrustServerCertificate=True";

    public ApiFactory()
    {
        DropTestDatabase();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<DbContextOptions>();
            services.RemoveAll<AppDbContext>();

            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(TestConnectionString));
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            DropTestDatabase();
        }
    }

    private static void DropTestDatabase()
    {
        using var connection = new SqlConnection(
            TestConnectionString.Replace("Database=SocialFeed_IntegrationTests", "Database=master"));

        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = """
            IF DB_ID('SocialFeed_IntegrationTests') IS NOT NULL
            BEGIN
                ALTER DATABASE [SocialFeed_IntegrationTests] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [SocialFeed_IntegrationTests];
            END
            """;

        command.ExecuteNonQuery();
    }
}
