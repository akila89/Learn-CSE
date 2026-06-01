using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Models;
using CseAnalyser.Scraper.Scraper.Phases;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace CseAnalyser.Scraper.Tests.Integration;

[Trait("Category", "Integration")]
[Collection("Integration")]
public sealed class TechnicalIndicatorsPhaseIntegrationTests : IAsyncDisposable
{
    private readonly NpgsqlDataSource _db = TestDb.Create();
    private readonly TechnicalIndicatorsPhase _phase;

    public TechnicalIndicatorsPhaseIntegrationTests()
    {
        _phase = new TechnicalIndicatorsPhase(_db, NullLogger.Instance);
    }

    [Fact]
    public async Task does_nothing_when_is_holiday()
    {
        ScraperRunContext context = new() { IsHoliday = true };

        await _phase.RunAsync(context);

        Assert.Empty(context.ErrorDetails);
    }

    [Fact]
    public async Task skips_stock_with_no_price_history()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertSeedAsync("NOPR.N0000");
            ScraperRunContext context = new();
            context.ActiveStocks.Add(new StockRow(stockId, "NOPR.N0000", null, null));

            await _phase.RunAsync(context);

            bool hasIndicators = await IndicatorsExistAsync(stockId);
            Assert.False(hasIndicators);
        }
        finally
        {
            await DeleteSeedAsync(Guid.Empty, stockId, companyId, watchlistId);
        }
    }

    [Fact]
    public async Task upserts_indicator_row_for_stock_with_prices()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertSeedAsync("INDIC.N0000");
            await InsertDailyPricesAsync(stockId, days: 30);
            ScraperRunContext context = new();
            context.ActiveStocks.Add(new StockRow(stockId, "INDIC.N0000", null, null));

            await _phase.RunAsync(context);

            bool hasIndicators = await IndicatorsExistAsync(stockId);
            Assert.True(hasIndicators);
        }
        finally
        {
            await DeleteSeedAsync(stockId, stockId, companyId, watchlistId);
        }
    }

    [Fact]
    public async Task rsi_is_null_when_fewer_than_15_prices()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertSeedAsync("RSIL.N0000");
            await InsertDailyPricesAsync(stockId, days: 14);
            ScraperRunContext context = new();
            context.ActiveStocks.Add(new StockRow(stockId, "RSIL.N0000", null, null));

            await _phase.RunAsync(context);

            decimal? rsi = await QueryRsiAsync(stockId);
            Assert.Null(rsi);
        }
        finally
        {
            await DeleteSeedAsync(stockId, stockId, companyId, watchlistId);
        }
    }

    [Fact]
    public async Task sma_200_is_null_when_fewer_than_200_prices()
    {
        (Guid companyId, Guid stockId, Guid watchlistId) = (Guid.Empty, Guid.Empty, Guid.Empty);
        try
        {
            (companyId, stockId, watchlistId) = await InsertSeedAsync("SMAL.N0000");
            await InsertDailyPricesAsync(stockId, days: 50);
            ScraperRunContext context = new();
            context.ActiveStocks.Add(new StockRow(stockId, "SMAL.N0000", null, null));

            await _phase.RunAsync(context);

            decimal? sma200 = await QuerySma200Async(stockId);
            Assert.Null(sma200);
        }
        finally
        {
            await DeleteSeedAsync(stockId, stockId, companyId, watchlistId);
        }
    }

    private async Task<(Guid CompanyId, Guid StockId, Guid WatchlistId)> InsertSeedAsync(string symbol)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        string symbolRoot = symbol.Split('.')[0];

        await using NpgsqlCommand cCmd = conn.CreateCommand();
        cCmd.CommandText = "INSERT INTO companies (name, symbol_root) VALUES (@n, @r) RETURNING id";
        cCmd.Parameters.AddWithValue("n", $"Test {symbol}");
        cCmd.Parameters.AddWithValue("r", symbolRoot);
        Guid companyId = (Guid)(await cCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand sCmd = conn.CreateCommand();
        sCmd.CommandText = "INSERT INTO stocks (company_id, symbol) VALUES (@c, @s) RETURNING id";
        sCmd.Parameters.AddWithValue("c", companyId);
        sCmd.Parameters.AddWithValue("s", symbol);
        Guid stockId = (Guid)(await sCmd.ExecuteScalarAsync())!;

        await using NpgsqlCommand wCmd = conn.CreateCommand();
        wCmd.CommandText = "INSERT INTO watchlist_entries (stock_id, is_active) VALUES (@s, true) RETURNING id";
        wCmd.Parameters.AddWithValue("s", stockId);
        Guid watchlistId = (Guid)(await wCmd.ExecuteScalarAsync())!;

        return (companyId, stockId, watchlistId);
    }

    private async Task InsertDailyPricesAsync(Guid stockId, int days)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        DateOnly baseDate = new(2026, 1, 1);

        for (int i = 0; i < days; i++)
        {
            DateOnly date = baseDate.AddDays(i);
            decimal close = 100m + i;

            await using NpgsqlCommand cmd = conn.CreateCommand();
            cmd.CommandText = """
                INSERT INTO daily_prices (stock_id, date, close, volume)
                VALUES (@stockId, @date, @close, 1000)
                ON CONFLICT (stock_id, date) DO NOTHING
                """;
            cmd.Parameters.AddWithValue("stockId", stockId);
            cmd.Parameters.AddWithValue("date", date.ToDateTime(TimeOnly.MinValue));
            cmd.Parameters.AddWithValue("close", close);
            await cmd.ExecuteNonQueryAsync();
        }
    }

    private async Task<bool> IndicatorsExistAsync(Guid stockId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM daily_indicators WHERE stock_id = @id";
        cmd.Parameters.AddWithValue("id", stockId);
        return (long)(await cmd.ExecuteScalarAsync())! > 0;
    }

    private async Task<decimal?> QueryRsiAsync(Guid stockId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT rsi_14 FROM daily_indicators WHERE stock_id = @id ORDER BY date DESC LIMIT 1";
        cmd.Parameters.AddWithValue("id", stockId);
        object? result = await cmd.ExecuteScalarAsync();
        return result is DBNull or null ? null : Convert.ToDecimal(result);
    }

    private async Task<decimal?> QuerySma200Async(Guid stockId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT sma_200 FROM daily_indicators WHERE stock_id = @id ORDER BY date DESC LIMIT 1";
        cmd.Parameters.AddWithValue("id", stockId);
        object? result = await cmd.ExecuteScalarAsync();
        return result is DBNull or null ? null : Convert.ToDecimal(result);
    }

    private async Task DeleteSeedAsync(Guid indicatorsStockId, Guid stockId, Guid companyId, Guid watchlistId)
    {
        await using NpgsqlConnection conn = await _db.OpenConnectionAsync();

        await ExecuteDeleteAsync(conn, "daily_indicators", "stock_id", indicatorsStockId);
        await ExecuteDeleteAsync(conn, "daily_prices", "stock_id", stockId);
        await ExecuteDeleteAsync(conn, "watchlist_entries", "id", watchlistId);
        await ExecuteDeleteAsync(conn, "stocks", "id", stockId);
        await ExecuteDeleteAsync(conn, "companies", "id", companyId);
    }

    private static async Task ExecuteDeleteAsync(NpgsqlConnection conn, string table, string col, Guid id)
    {
        if (id == Guid.Empty) return;
        await using NpgsqlCommand cmd = conn.CreateCommand();
        cmd.CommandText = $"DELETE FROM {table} WHERE {col} = @id";
        cmd.Parameters.AddWithValue("id", id);
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync() => await _db.DisposeAsync();
}
