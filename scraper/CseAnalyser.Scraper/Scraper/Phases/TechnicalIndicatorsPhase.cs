using CseAnalyser.Scraper.Scraper.Models;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class TechnicalIndicatorsPhase(NpgsqlDataSource db, ILogger logger)
{
    public async Task RunAsync(ScraperRunContext context, CancellationToken ct = default)
    {
        if (context.IsHoliday)
            return;

        logger.LogInformation("TechnicalIndicators starting — {Count} stocks", context.ActiveStocks.Count);

        int processed = 0;
        foreach (StockRow stock in context.ActiveStocks)
        {
            try
            {
                await ComputeAndUpsertAsync(stock, ct);
                processed++;
            }
            catch (Exception ex)
            {
                context.ErrorDetails.Add($"{stock.Symbol} indicators: {ex.Message}");
            }
        }

        logger.LogInformation("TechnicalIndicators done — {Processed}/{Total} processed",
            processed, context.ActiveStocks.Count);
    }

    private async Task ComputeAndUpsertAsync(StockRow stock, CancellationToken ct)
    {
        (DateOnly date, List<decimal> prices) = await LoadPricesAsync(stock.Id, ct);
        if (prices.Count == 0)
            return;

        IndicatorRow row = TechnicalIndicatorsCalculator.Compute(stock.Id, date, prices);
        await UpsertIndicatorAsync(row, ct);
    }

    private async Task<(DateOnly LatestDate, List<decimal> ClosePrices)> LoadPricesAsync(
        Guid stockId,
        CancellationToken ct)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT date, close
            FROM daily_prices
            WHERE stock_id = @stockId
            ORDER BY date ASC
            """;
        cmd.Parameters.AddWithValue("stockId", stockId);

        List<decimal> prices = [];
        DateOnly latestDate = default;

        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            latestDate = DateOnly.FromDateTime(reader.GetDateTime(0));
            prices.Add(reader.GetDecimal(1));
        }

        return (latestDate, prices);
    }

    private async Task UpsertIndicatorAsync(IndicatorRow row, CancellationToken ct)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO daily_indicators
                (stock_id, date,
                 rsi_14,
                 macd_line, macd_signal, macd_histogram,
                 sma_50, sma_200,
                 bb_upper, bb_mid, bb_lower,
                 week_52_high, week_52_low)
            VALUES
                (@stockId, @date,
                 @rsi14,
                 @macdLine, @macdSignal, @macdHistogram,
                 @sma50, @sma200,
                 @bbUpper, @bbMid, @bbLower,
                 @week52High, @week52Low)
            ON CONFLICT (stock_id, date) DO UPDATE
                SET rsi_14         = EXCLUDED.rsi_14,
                    macd_line      = EXCLUDED.macd_line,
                    macd_signal    = EXCLUDED.macd_signal,
                    macd_histogram = EXCLUDED.macd_histogram,
                    sma_50         = EXCLUDED.sma_50,
                    sma_200        = EXCLUDED.sma_200,
                    bb_upper       = EXCLUDED.bb_upper,
                    bb_mid         = EXCLUDED.bb_mid,
                    bb_lower       = EXCLUDED.bb_lower,
                    week_52_high   = EXCLUDED.week_52_high,
                    week_52_low    = EXCLUDED.week_52_low
            """;
        cmd.Parameters.AddWithValue("stockId", row.StockId);
        cmd.Parameters.AddWithValue("date", row.Date.ToDateTime(TimeOnly.MinValue));
        cmd.Parameters.AddWithValue("rsi14", (object?)row.Rsi14 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("macdLine", (object?)row.MacdLine ?? DBNull.Value);
        cmd.Parameters.AddWithValue("macdSignal", (object?)row.MacdSignal ?? DBNull.Value);
        cmd.Parameters.AddWithValue("macdHistogram", (object?)row.MacdHistogram ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sma50", (object?)row.Sma50 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("sma200", (object?)row.Sma200 ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bbUpper", (object?)row.BbUpper ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bbMid", (object?)row.BbMid ?? DBNull.Value);
        cmd.Parameters.AddWithValue("bbLower", (object?)row.BbLower ?? DBNull.Value);
        cmd.Parameters.AddWithValue("week52High", (object?)row.Week52High ?? DBNull.Value);
        cmd.Parameters.AddWithValue("week52Low", (object?)row.Week52Low ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
