using CseAnalyser.Scraper.Scraper;
using CseAnalyser.Scraper.Scraper.Models;

namespace CseAnalyser.Scraper.Tests.Scraper;

public class TechnicalIndicatorsCalculatorTests
{
    // ── Null-window edge cases ──────────────────────────────────────────────

    [Fact]
    public void rsi_is_null_when_fewer_than_15_prices()
    {
        decimal? rsi = TechnicalIndicatorsCalculator.ComputeRsi(Prices(14));

        Assert.Null(rsi);
    }

    [Fact]
    public void macd_line_is_null_when_fewer_than_26_prices()
    {
        (decimal? line, decimal? signal, decimal? histogram) =
            TechnicalIndicatorsCalculator.ComputeMacd(Prices(25));

        Assert.Null(line);
        Assert.Null(signal);
        Assert.Null(histogram);
    }

    [Fact]
    public void sma_200_is_null_when_fewer_than_200_prices()
    {
        decimal? sma200 = TechnicalIndicatorsCalculator.ComputeSma(Prices(199), 200);

        Assert.Null(sma200);
    }

    [Fact]
    public void sma_50_is_null_when_fewer_than_50_prices()
    {
        decimal? sma50 = TechnicalIndicatorsCalculator.ComputeSma(Prices(49), 50);

        Assert.Null(sma50);
    }

    [Fact]
    public void bollinger_bands_are_null_when_fewer_than_20_prices()
    {
        (decimal? upper, decimal? mid, decimal? lower) =
            TechnicalIndicatorsCalculator.ComputeBollingerBands(Prices(19));

        Assert.Null(upper);
        Assert.Null(mid);
        Assert.Null(lower);
    }

    // ── RSI exact values ────────────────────────────────────────────────────

    [Fact]
    public void rsi_returns_value_with_exactly_15_prices()
    {
        decimal? rsi = TechnicalIndicatorsCalculator.ComputeRsi(Prices(15));

        Assert.NotNull(rsi);
    }

    [Fact]
    public void rsi_is_100_when_all_gains_no_losses()
    {
        // Strictly ascending prices produce avgLoss = 0 → RSI = 100
        decimal[] prices = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];

        decimal? rsi = TechnicalIndicatorsCalculator.ComputeRsi(prices);

        Assert.Equal(100m, rsi);
    }

    [Fact]
    public void rsi_is_0_when_all_losses_no_gains()
    {
        // Strictly descending: avgGain = 0, RS = 0 → RSI = 0
        decimal[] prices = [15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1];

        decimal? rsi = TechnicalIndicatorsCalculator.ComputeRsi(prices);

        Assert.Equal(0m, rsi);
    }

    [Fact]
    public void rsi_known_sequence_returns_expected_value()
    {
        // 15 closes: alternating +1 / -0.5 pattern
        // Gains over first 14 steps (indices 1–14 vs 0–13):
        //   Even indices (1,3,5,7,9,11,13): price[i] - price[i-1] = +1 → 7 gains
        //   Odd  indices (2,4,6,8,10,12,14): price[i] - price[i-1] = -0.5 → 7 losses
        // avgGain = (7 * 1.0) / 14 = 0.5
        // avgLoss = (7 * 0.5) / 14 = 0.25
        // RS = 0.5 / 0.25 = 2.0
        // RSI = 100 - 100 / (1 + 2) = 66.666...
        decimal[] prices = [10, 11, 10.5m, 11.5m, 11, 12, 11.5m, 12.5m, 12, 13, 12.5m, 13.5m, 13, 14, 13.5m];

        decimal? rsi = TechnicalIndicatorsCalculator.ComputeRsi(prices);

        Assert.NotNull(rsi);
        Assert.InRange(rsi!.Value, 66m, 68m);
    }

    // ── SMA exact values ────────────────────────────────────────────────────

    [Fact]
    public void sma_50_returns_average_of_last_50_prices()
    {
        decimal[] prices = Prices(100).ToArray();
        // Last 50 are indices 50–99 = values 51, 52, ..., 100
        // Average = (51 + 100) / 2 = 75.5
        decimal? sma = TechnicalIndicatorsCalculator.ComputeSma(prices, 50);

        Assert.Equal(75.5m, sma);
    }

    [Fact]
    public void sma_200_returns_average_of_last_200_prices()
    {
        decimal[] prices = Prices(200).ToArray();
        // Values 1–200, average = 100.5
        decimal? sma = TechnicalIndicatorsCalculator.ComputeSma(prices, 200);

        Assert.Equal(100.5m, sma);
    }

    // ── Bollinger Bands ──────────────────────────────────────────────────────

    [Fact]
    public void bollinger_mid_equals_sma_20()
    {
        decimal[] prices = Prices(30).ToArray();
        decimal? expectedMid = TechnicalIndicatorsCalculator.ComputeSma(prices, 20);

        (decimal? _, decimal? mid, decimal? _) =
            TechnicalIndicatorsCalculator.ComputeBollingerBands(prices);

        Assert.Equal(expectedMid, mid);
    }

    [Fact]
    public void bollinger_upper_above_mid_lower_below()
    {
        decimal[] prices = Prices(25).ToArray();

        (decimal? upper, decimal? mid, decimal? lower) =
            TechnicalIndicatorsCalculator.ComputeBollingerBands(prices);

        Assert.NotNull(upper);
        Assert.NotNull(mid);
        Assert.NotNull(lower);
        Assert.True(upper > mid);
        Assert.True(lower < mid);
    }

    [Fact]
    public void bollinger_bands_are_equal_when_all_prices_identical()
    {
        decimal[] prices = Enumerable.Repeat(100m, 25).ToArray();

        (decimal? upper, decimal? mid, decimal? lower) =
            TechnicalIndicatorsCalculator.ComputeBollingerBands(prices);

        Assert.Equal(100m, mid);
        Assert.Equal(100m, upper);
        Assert.Equal(100m, lower);
    }

    // ── MACD ─────────────────────────────────────────────────────────────────

    [Fact]
    public void macd_line_not_null_with_26_prices()
    {
        (decimal? line, _, _) = TechnicalIndicatorsCalculator.ComputeMacd(Prices(26));

        Assert.NotNull(line);
    }

    [Fact]
    public void macd_signal_is_null_when_fewer_than_34_prices()
    {
        // Need MacdSlow(26) + MacdSignal(9) - 1 = 34 prices for signal
        (decimal? _, decimal? signal, decimal? _) =
            TechnicalIndicatorsCalculator.ComputeMacd(Prices(33));

        Assert.Null(signal);
    }

    [Fact]
    public void macd_signal_not_null_with_34_prices()
    {
        (decimal? _, decimal? signal, decimal? _) =
            TechnicalIndicatorsCalculator.ComputeMacd(Prices(34));

        Assert.NotNull(signal);
    }

    [Fact]
    public void macd_histogram_equals_line_minus_signal()
    {
        (decimal? line, decimal? signal, decimal? histogram) =
            TechnicalIndicatorsCalculator.ComputeMacd(Prices(50));

        Assert.NotNull(line);
        Assert.NotNull(signal);
        Assert.NotNull(histogram);
        Assert.Equal(line!.Value - signal!.Value, histogram!.Value, precision: 8);
    }

    // ── 52-week high/low ────────────────────────────────────────────────────

    [Fact]
    public void week52_high_and_low_are_null_for_empty_price_list()
    {
        (decimal? high, decimal? low) =
            TechnicalIndicatorsCalculator.ComputeWeek52HighLow([]);

        Assert.Null(high);
        Assert.Null(low);
    }

    [Fact]
    public void week52_high_is_max_of_last_252_prices()
    {
        // 300 prices: 1..300. Last 252 = 49..300 → max = 300
        decimal[] prices = Prices(300).ToArray();

        (decimal? high, decimal? low) = TechnicalIndicatorsCalculator.ComputeWeek52HighLow(prices);

        Assert.Equal(300m, high);
    }

    [Fact]
    public void week52_low_is_min_of_last_252_prices()
    {
        decimal[] prices = Prices(300).ToArray();

        (decimal? high, decimal? low) = TechnicalIndicatorsCalculator.ComputeWeek52HighLow(prices);

        // Prices 1..300; last 252 = indices 48..299 → values 49..300
        Assert.Equal(49m, low);
    }

    // ── Compute (integration of all indicators) ──────────────────────────────

    [Fact]
    public void compute_returns_nulls_for_insufficient_history()
    {
        Guid stockId = Guid.NewGuid();
        DateOnly date = new(2026, 5, 1);
        decimal[] prices = Prices(10).ToArray();

        IndicatorRow row = TechnicalIndicatorsCalculator.Compute(stockId, date, prices);

        Assert.Equal(stockId, row.StockId);
        Assert.Equal(date, row.Date);
        Assert.Null(row.Rsi14);
        Assert.Null(row.MacdLine);
        Assert.Null(row.Sma50);
        Assert.Null(row.Sma200);
        Assert.Null(row.BbUpper);
    }

    [Fact]
    public void compute_populates_all_indicators_with_sufficient_history()
    {
        Guid stockId = Guid.NewGuid();
        DateOnly date = new(2026, 5, 1);
        decimal[] prices = Prices(250).ToArray();

        IndicatorRow row = TechnicalIndicatorsCalculator.Compute(stockId, date, prices);

        Assert.NotNull(row.Rsi14);
        Assert.NotNull(row.MacdLine);
        Assert.NotNull(row.Sma50);
        Assert.NotNull(row.Week52High);
        Assert.NotNull(row.Week52Low);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static IReadOnlyList<decimal> Prices(int count) =>
        Enumerable.Range(1, count).Select(i => (decimal)i).ToList();
}
