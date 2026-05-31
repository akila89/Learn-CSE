using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Phases;
using Npgsql;

namespace CseAnalyser.Scraper.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class ActiveStocksLoadPhaseIntegrationTests : IAsyncDisposable
{
    private readonly NpgsqlDataSource _db = TestDb.Create();
    private readonly ActiveStocksLoadPhase _phase;

    public ActiveStocksLoadPhaseIntegrationTests()
    {
        _phase = new ActiveStocksLoadPhase(_db);
    }

    [Fact]
    public async Task returns_active_stock_with_correct_fields()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertActiveStockAsync("TEST.N0000", cseChartId: 999);
            ScraperRunContext context = new();

            await _phase.RunAsync(context);

            Assert.Single(context.ActiveStocks, s => s.Symbol == "TEST.N0000" && s.CseChartId == 999);
        }
        finally
        {
            await DeleteSeedAsync(watchlistId, stockId, companyId);
        }
    }

    [Fact]
    public async Task excludes_inactive_watchlist_entries()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertInactiveStockAsync("INACT.N0000");
            ScraperRunContext context = new();

            await _phase.RunAsync(context);

            Assert.DoesNotContain(context.ActiveStocks, s => s.Symbol == "INACT.N0000");
        }
        finally
        {
            await DeleteSeedAsync(watchlistId, stockId, companyId);
        }
    }

    private async Task<(Guid CompanyId, Guid StockId, Guid WatchlistId)> InsertActiveStockAsync(
        string symbol,
        int cseChartId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        string symbolRoot = symbol.Split('.')[0];

        await using NpgsqlCommand companyCmd = conn.CreateCommand();
        companyCmd.CommandText = "INSERT INTO companies (name, symbol_root) VALUES (@name, @root) RETURNING id";
        companyCmd.Parameters.AddWithValue("name", $"Test Company {symbol}");
        companyCmd.Parameters.AddWithValue("root", symbolRoot);
        Guid companyId = (Guid)(await companyCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand stockCmd = conn.CreateCommand();
        stockCmd.CommandText = "INSERT INTO stocks (company_id, symbol, cse_chart_id) VALUES (@cid, @sym, @chartId) RETURNING id";
        stockCmd.Parameters.AddWithValue("cid", companyId);
        stockCmd.Parameters.AddWithValue("sym", symbol);
        stockCmd.Parameters.AddWithValue("chartId", cseChartId);
        Guid stockId = (Guid)(await stockCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand wCmd = conn.CreateCommand();
        wCmd.CommandText = "INSERT INTO watchlist_entries (stock_id, is_active) VALUES (@sid, true) RETURNING id";
        wCmd.Parameters.AddWithValue("sid", stockId);
        Guid watchlistId = (Guid)(await wCmd.ExecuteScalarAsync())!;

        return (companyId, stockId, watchlistId);
    }

    private async Task<(Guid CompanyId, Guid StockId, Guid WatchlistId)> InsertInactiveStockAsync(string symbol)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        string symbolRoot = symbol.Split('.')[0];

        await using NpgsqlCommand companyCmd = conn.CreateCommand();
        companyCmd.CommandText = "INSERT INTO companies (name, symbol_root) VALUES (@name, @root) RETURNING id";
        companyCmd.Parameters.AddWithValue("name", $"Test Company {symbol}");
        companyCmd.Parameters.AddWithValue("root", symbolRoot);
        Guid companyId = (Guid)(await companyCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand stockCmd = conn.CreateCommand();
        stockCmd.CommandText = "INSERT INTO stocks (company_id, symbol) VALUES (@cid, @sym) RETURNING id";
        stockCmd.Parameters.AddWithValue("cid", companyId);
        stockCmd.Parameters.AddWithValue("sym", symbol);
        Guid stockId = (Guid)(await stockCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand wCmd = conn.CreateCommand();
        wCmd.CommandText = "INSERT INTO watchlist_entries (stock_id, is_active) VALUES (@sid, false) RETURNING id";
        wCmd.Parameters.AddWithValue("sid", stockId);
        Guid watchlistId = (Guid)(await wCmd.ExecuteScalarAsync())!;

        return (companyId, stockId, watchlistId);
    }

    private async Task DeleteSeedAsync(Guid watchlistId, Guid stockId, Guid companyId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();

        foreach ((string table, string col, Guid id) in new[]
        {
            ("watchlist_entries", "id", watchlistId),
            ("stocks", "id", stockId),
            ("companies", "id", companyId),
        })
        {
            if (id == Guid.Empty) continue;
            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = $"DELETE FROM {table} WHERE {col} = @id";
            cmd.Parameters.AddWithValue("id", id);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();
}
