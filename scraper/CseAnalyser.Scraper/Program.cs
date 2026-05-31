using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Phases;
using Microsoft.Extensions.Configuration;
using Npgsql;

IConfiguration config = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.Development.json", optional: true)
    .Build();

string connectionString =
    Environment.GetEnvironmentVariable("SCRAPER_DB_CONNECTION")
    ?? config.GetConnectionString("Scraper")
    ?? throw new InvalidOperationException(
        "No connection string found. Set SCRAPER_DB_CONNECTION or fill ConnectionStrings:Scraper in appsettings.json.");

NpgsqlDataSource db = NpgsqlDataSource.Create(connectionString);

HttpClient httpClient = new();
CseApiClient apiClient = new(httpClient);

ScraperPipeline pipeline = new(
    new ScraperRunLoggerPhase(db),
    new CseAllStocksRefreshPhase(apiClient, db),
    new ActiveStocksLoadPhase(db),
    new HolidayDetectionPhase(apiClient, db),
    new PriceScraperPhase(apiClient, db),
    new TechnicalIndicatorsPhase(db)
);

await pipeline.RunAsync();
