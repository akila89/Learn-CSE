using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CseAnalyser.Scraper.Tests.Integration;

internal static class TestDb
{
    private static readonly string ConnectionString = BuildConnectionString();

    internal static NpgsqlDataSource Create() => NpgsqlDataSource.Create(ConnectionString);

    private static string BuildConnectionString()
    {
        IConfiguration config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.Test.json", optional: true)
            .Build();

        return Environment.GetEnvironmentVariable("SCRAPER_TEST_DB_CONNECTION")
            ?? config.GetConnectionString("ScraperTest")
            ?? throw new InvalidOperationException(
                "No test connection string found. Add ConnectionStrings:ScraperTest to appsettings.Test.json.");
    }
}
