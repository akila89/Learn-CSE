using CseAnalyser.Scraper.Scraper.Models;

namespace CseAnalyser.Scraper.Scraper;

public sealed class ScraperRunContext
{
    public Guid RunId { get; set; }
    public bool IsHoliday { get; set; }
    public List<StockRow> ActiveStocks { get; set; } = [];
    public int StocksAttempted { get; set; }
    public int StocksFailed { get; set; }
    public List<string> ErrorDetails { get; } = [];

    public string FinalStatus =>
        IsHoliday ? "holiday" :
        StocksFailed == 0 ? "success" :
        StocksAttempted > 0 && StocksFailed < StocksAttempted ? "partial" :
        "failed";
}
