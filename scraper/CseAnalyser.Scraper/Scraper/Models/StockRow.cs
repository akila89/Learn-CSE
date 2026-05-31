namespace CseAnalyser.Scraper.Scraper.Models;

public record StockRow(
    Guid Id,
    string Symbol,
    int? CseChartId,
    DateOnly? LastPriceScrapedAt
);
