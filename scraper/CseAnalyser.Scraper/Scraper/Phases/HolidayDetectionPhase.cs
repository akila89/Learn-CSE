using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.CseApi.Dto;
using CseAnalyser.Scraper.Scraper.Models;
using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class HolidayDetectionPhase(CseApiClient apiClient, NpgsqlDataSource db)
{
    public async Task RunAsync(ScraperRunContext context, CancellationToken ct = default)
    {
        if (context.ActiveStocks.Count == 0)
            return;

        StockRow probe = context.ActiveStocks[0];
        if (probe.CseChartId is null)
            return;

        CseApiResult<IReadOnlyList<OhlcvDataPointDto>> result =
            await apiClient.GetChartDataAsync(probe.CseChartId.Value, ct: ct);

        if (!result.IsSuccess || result.Value!.Count == 0)
            return;

        DateOnly latestApiDate = TimestampToDate(result.Value.Max(p => p.TimestampMs));
        DateOnly? storedDate = await GetLatestStoredDateAsync(probe.Id, ct);

        if (storedDate.HasValue && latestApiDate <= storedDate.Value)
            context.IsHoliday = true;
    }

    private async Task<DateOnly?> GetLatestStoredDateAsync(Guid stockId, CancellationToken ct)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(date) FROM daily_prices WHERE stock_id = @stockId";
        cmd.Parameters.AddWithValue("stockId", stockId);
        object? scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null or DBNull)
            return null;
        return DateOnly.FromDateTime((DateTime)scalar);
    }

    private static DateOnly TimestampToDate(long timestampMs) =>
        DateOnly.FromDateTime(DateTimeOffset.FromUnixTimeMilliseconds(timestampMs).UtcDateTime);
}
