using CseAnalyser.Scraper.Scraper.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class ActiveStocksLoadPhase(NpgsqlDataSource db, ILogger logger)
{
    public async Task RunAsync(ScraperRunContext context, CancellationToken ct = default)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT s.id, s.symbol, s.cse_chart_id,
                   (s.last_price_scraped_at AT TIME ZONE 'UTC')::date AS last_date
            FROM stocks s
            INNER JOIN watchlist_entries w ON w.stock_id = s.id
            WHERE w.is_active = true
            ORDER BY s.symbol
            """;

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        List<StockRow> stocks = [];
        while (await reader.ReadAsync(ct))
        {
            DateOnly? lastDate = reader.IsDBNull(3) ? null : DateOnly.FromDateTime(reader.GetDateTime(3));
            stocks.Add(new StockRow(
                reader.GetGuid(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetInt32(2),
                lastDate
            ));
        }

        context.ActiveStocks = stocks;
        logger.LogInformation("ActiveStocksLoad — {Count} stocks loaded", stocks.Count);
    }
}
