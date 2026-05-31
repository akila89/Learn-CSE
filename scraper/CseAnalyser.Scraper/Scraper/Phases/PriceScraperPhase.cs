using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.CseApi.Dto;
using CseAnalyser.Scraper.Scraper.Models;
using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class PriceScraperPhase(CseApiClient apiClient, NpgsqlDataSource db)
{
    public async Task RunAsync(ScraperRunContext context, CancellationToken ct = default)
    {
        if (context.IsHoliday)
            return;

        foreach (StockRow stock in context.ActiveStocks)
        {
            if (stock.CseChartId is null)
                continue;

            context.StocksAttempted++;

            int period = stock.LastPriceScrapedAt is null ? 5 : 1;

            CseApiResult<IReadOnlyList<OhlcvDataPointDto>> result =
                await apiClient.GetChartDataAsync(stock.CseChartId.Value, period, ct);

            if (!result.IsSuccess)
            {
                context.StocksFailed++;
                context.ErrorDetails.Add($"{stock.Symbol}: {result.Error!.Message}");
                continue;
            }

            try
            {
                await UpsertPricesAsync(stock.Id, result.Value!, ct);
            }
            catch (Exception ex)
            {
                context.StocksFailed++;
                context.ErrorDetails.Add($"{stock.Symbol}: {ex.Message}");
            }
        }
    }

    private async Task UpsertPricesAsync(
        Guid stockId,
        IReadOnlyList<OhlcvDataPointDto> dataPoints,
        CancellationToken ct)
    {
        if (dataPoints.Count == 0)
            return;

        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);

        foreach (OhlcvDataPointDto point in dataPoints)
        {
            DateOnly date = DateOnly.FromDateTime(
                DateTimeOffset.FromUnixTimeMilliseconds(point.TimestampMs).UtcDateTime);

            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO daily_prices
                    (stock_id, date, open, high, low, close, volume, change, change_pct)
                VALUES
                    (@stockId, @date, @open, @high, @low, @close, @volume, @change, @changePct)
                ON CONFLICT (stock_id, date) DO UPDATE
                    SET open       = EXCLUDED.open,
                        high       = EXCLUDED.high,
                        low        = EXCLUDED.low,
                        close      = EXCLUDED.close,
                        volume     = EXCLUDED.volume,
                        change     = EXCLUDED.change,
                        change_pct = EXCLUDED.change_pct
                """;
            cmd.Parameters.AddWithValue("stockId", stockId);
            cmd.Parameters.AddWithValue("date", date);
            cmd.Parameters.AddWithValue("open", (object?)point.Open ?? DBNull.Value);
            cmd.Parameters.AddWithValue("high", (object?)point.High ?? DBNull.Value);
            cmd.Parameters.AddWithValue("low", (object?)point.Low ?? DBNull.Value);
            cmd.Parameters.AddWithValue("close", point.Close);
            cmd.Parameters.AddWithValue("volume", point.Volume);
            cmd.Parameters.AddWithValue("change", (object?)point.Change ?? DBNull.Value);
            cmd.Parameters.AddWithValue("changePct", (object?)point.PercentChange ?? DBNull.Value);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);

        await UpdateLastScrapedAtAsync(stockId, dataPoints.Max(p => p.TimestampMs), conn, ct);
    }

    private static async Task UpdateLastScrapedAtAsync(
        Guid stockId,
        long latestTimestampMs,
        NpgsqlConnection conn,
        CancellationToken ct)
    {
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE stocks
            SET last_price_scraped_at = @ts
            WHERE id = @stockId
              AND (last_price_scraped_at IS NULL OR last_price_scraped_at < @ts)
            """;
        cmd.Parameters.AddWithValue("ts", DateTimeOffset.FromUnixTimeMilliseconds(latestTimestampMs).UtcDateTime);
        cmd.Parameters.AddWithValue("stockId", stockId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
