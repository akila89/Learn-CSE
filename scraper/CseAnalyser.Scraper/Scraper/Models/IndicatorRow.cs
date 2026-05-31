namespace CseAnalyser.Scraper.Scraper.Models;

public record IndicatorRow(
    Guid StockId,
    DateOnly Date,
    decimal? Rsi14,
    decimal? MacdLine,
    decimal? MacdSignal,
    decimal? MacdHistogram,
    decimal? Sma50,
    decimal? Sma200,
    decimal? BbUpper,
    decimal? BbMid,
    decimal? BbLower,
    decimal? Week52High,
    decimal? Week52Low
);
