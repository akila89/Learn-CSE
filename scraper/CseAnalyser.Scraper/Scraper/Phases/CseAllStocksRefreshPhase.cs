using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.CseApi.Dto;
using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class CseAllStocksRefreshPhase(CseApiClient apiClient, NpgsqlDataSource db)
{
    private const int RefreshIntervalDays = 7;

    public async Task RunAsync(CancellationToken ct = default)
    {
        if (!await IsRefreshDueAsync(ct))
            return;

        CseApiResult<IReadOnlyList<SecurityCodeDto>> result = await apiClient.GetAllSecurityCodesAsync(ct);
        if (!result.IsSuccess)
            return;

        await UpsertAsync(result.Value!, ct);
    }

    private async Task<bool> IsRefreshDueAsync(CancellationToken ct)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT MAX(refreshed_at) FROM cse_all_stocks";
        object? scalar = await cmd.ExecuteScalarAsync(ct);
        if (scalar is null or DBNull)
            return true;

        DateTime lastRefresh = (DateTime)scalar;
        return (DateTime.UtcNow - lastRefresh).TotalDays >= RefreshIntervalDays;
    }

    private async Task UpsertAsync(IReadOnlyList<SecurityCodeDto> stocks, CancellationToken ct)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlTransaction tx = await conn.BeginTransactionAsync(ct);

        foreach (SecurityCodeDto stock in stocks)
        {
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.Transaction = tx;
            cmd.CommandText = """
                INSERT INTO cse_all_stocks (id, symbol, name, refreshed_at)
                VALUES (@id, @symbol, @name, now())
                ON CONFLICT (id) DO UPDATE
                    SET symbol = EXCLUDED.symbol,
                        name   = EXCLUDED.name,
                        refreshed_at = now()
                """;
            cmd.Parameters.AddWithValue("id", stock.CseChartId);
            cmd.Parameters.AddWithValue("symbol", stock.Symbol);
            cmd.Parameters.AddWithValue("name", stock.Name);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await tx.CommitAsync(ct);
    }
}
