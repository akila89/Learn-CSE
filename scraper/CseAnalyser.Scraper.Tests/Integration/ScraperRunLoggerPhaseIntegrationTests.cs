using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Phases;
using Npgsql;

namespace CseAnalyser.Scraper.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class ScraperRunLoggerPhaseIntegrationTests : IAsyncDisposable
{
    private readonly NpgsqlDataSource _db = TestDb.Create();
    private readonly ScraperRunLoggerPhase _phase;

    public ScraperRunLoggerPhaseIntegrationTests()
    {
        _phase = new ScraperRunLoggerPhase(_db);
    }

    [Fact]
    public async Task start_inserts_row_with_partial_status()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();

            string? status = await QueryStatusAsync(runId);
            Assert.Equal("partial", status);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    [Fact]
    public async Task complete_updates_row_to_success_when_no_failures()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();
            ScraperRunContext context = new() { RunId = runId, StocksAttempted = 5, StocksFailed = 0 };

            await _phase.CompleteAsync(context);

            string? status = await QueryStatusAsync(runId);
            Assert.Equal("success", status);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    [Fact]
    public async Task complete_updates_row_to_partial_when_some_failures()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();
            ScraperRunContext context = new() { RunId = runId, StocksAttempted = 5, StocksFailed = 2 };

            await _phase.CompleteAsync(context);

            string? status = await QueryStatusAsync(runId);
            Assert.Equal("partial", status);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    [Fact]
    public async Task complete_updates_row_to_holiday_when_is_holiday()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();
            ScraperRunContext context = new() { RunId = runId, IsHoliday = true };

            await _phase.CompleteAsync(context);

            string? status = await QueryStatusAsync(runId);
            Assert.Equal("holiday", status);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    [Fact]
    public async Task complete_stores_error_detail_when_stocks_failed()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();
            ScraperRunContext context = new() { RunId = runId, StocksAttempted = 1, StocksFailed = 1 };
            context.ErrorDetails.Add("JKH.N0000: timeout");

            await _phase.CompleteAsync(context);

            string? errorDetail = await QueryErrorDetailAsync(runId);
            Assert.Equal("JKH.N0000: timeout", errorDetail);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    [Fact]
    public async Task complete_stores_stocks_attempted_and_failed_counts()
    {
        Guid runId = Guid.Empty;
        try
        {
            runId = await _phase.StartAsync();
            ScraperRunContext context = new() { RunId = runId, StocksAttempted = 10, StocksFailed = 3 };

            await _phase.CompleteAsync(context);

            (int attempted, int failed) = await QueryCountsAsync(runId);
            Assert.Equal(10, attempted);
            Assert.Equal(3, failed);
        }
        finally
        {
            await DeleteRunAsync(runId);
        }
    }

    private async Task<string?> QueryStatusAsync(Guid runId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT status FROM scraper_runs WHERE id = @id";
        cmd.Parameters.AddWithValue("id", runId);
        return (string?)await cmd.ExecuteScalarAsync();
    }

    private async Task<string?> QueryErrorDetailAsync(Guid runId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT error_detail FROM scraper_runs WHERE id = @id";
        cmd.Parameters.AddWithValue("id", runId);
        return (string?)await cmd.ExecuteScalarAsync();
    }

    private async Task<(int Attempted, int Failed)> QueryCountsAsync(Guid runId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT stocks_attempted, stocks_failed FROM scraper_runs WHERE id = @id";
        cmd.Parameters.AddWithValue("id", runId);
        await using NpgsqlDataReader reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        return (reader.GetInt32(0), reader.GetInt32(1));
    }

    private async Task DeleteRunAsync(Guid runId)
    {
        if (runId == Guid.Empty) return;
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM scraper_runs WHERE id = @id";
        cmd.Parameters.AddWithValue("id", runId);
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();
}
