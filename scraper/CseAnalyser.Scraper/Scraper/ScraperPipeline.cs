using CseAnalyser.Scraper.Scraper.Phases;

namespace CseAnalyser.Scraper.Scraper;

public sealed class ScraperPipeline(
    ScraperRunLoggerPhase runLogger,
    CseAllStocksRefreshPhase allStocksRefresh,
    ActiveStocksLoadPhase activeStocksLoad,
    HolidayDetectionPhase holidayDetection,
    PriceScraperPhase priceScraper,
    TechnicalIndicatorsPhase technicalIndicators)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        ScraperRunContext context = new();
        context.RunId = await runLogger.StartAsync(ct);

        try
        {
            await allStocksRefresh.RunAsync(ct);
            await activeStocksLoad.RunAsync(context, ct);
            await holidayDetection.RunAsync(context, ct);
            await priceScraper.RunAsync(context, ct);
            await technicalIndicators.RunAsync(context, ct);
        }
        finally
        {
            await runLogger.CompleteAsync(context, ct);
        }
    }
}
