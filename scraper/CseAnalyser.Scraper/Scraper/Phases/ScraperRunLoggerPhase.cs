using Npgsql;

namespace CseAnalyser.Scraper.Scraper.Phases;

public sealed class ScraperRunLoggerPhase(NpgsqlDataSource db)
{
    public async Task<Guid> StartAsync(CancellationToken ct = default)
    {
        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO scraper_runs (status)
            VALUES ('partial')
            RETURNING id
            """;
        Guid runId = (Guid)(await cmd.ExecuteScalarAsync(ct))!;
        return runId;
    }

    public async Task CompleteAsync(ScraperRunContext context, CancellationToken ct = default)
    {
        string errorDetail = context.ErrorDetails.Count > 0
            ? string.Join("\n", context.ErrorDetails)
            : string.Empty;

        await using NpgsqlConnection conn = await db.OpenConnectionAsync(ct);
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE scraper_runs
            SET status            = @status,
                stocks_attempted  = @attempted,
                stocks_failed     = @failed,
                error_detail      = NULLIF(@errorDetail, '')
            WHERE id = @runId
            """;
        cmd.Parameters.AddWithValue("status", context.FinalStatus);
        cmd.Parameters.AddWithValue("attempted", context.StocksAttempted);
        cmd.Parameters.AddWithValue("failed", context.StocksFailed);
        cmd.Parameters.AddWithValue("errorDetail", errorDetail);
        cmd.Parameters.AddWithValue("runId", context.RunId);
        await cmd.ExecuteNonQueryAsync(ct);
    }
}
