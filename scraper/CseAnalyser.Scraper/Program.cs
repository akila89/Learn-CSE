using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Phases;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

using ILoggerFactory loggerFactory = LoggerFactory.Create(b =>
    b.AddConfiguration(config.GetSection("Logging"))
     .AddConsole());

NpgsqlDataSource db = NpgsqlDataSource.Create(connectionString);

HttpClient httpClient = new();
CseApiClient apiClient = new(httpClient);

ScraperPipeline pipeline = new(
    new ScraperRunLoggerPhase(db, loggerFactory.CreateLogger("ScraperRunLogger")),
    new CseAllStocksRefreshPhase(apiClient, db, loggerFactory.CreateLogger("AllStocksRefresh")),
    new ActiveStocksLoadPhase(db, loggerFactory.CreateLogger("ActiveStocksLoad")),
    new HolidayDetectionPhase(apiClient, db, loggerFactory.CreateLogger("HolidayDetection")),
    new PriceScraperPhase(apiClient, db, loggerFactory.CreateLogger("PriceScraper")),
    new TechnicalIndicatorsPhase(db, loggerFactory.CreateLogger("TechnicalIndicators"))
);

await pipeline.RunAsync();
