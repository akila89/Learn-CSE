using CseAnalyser.Scraper.Scraper.Models;

namespace CseAnalyser.Scraper.Scraper;

public static class TechnicalIndicatorsCalculator
{
    private const int RsiPeriod = 14;
    private const int MacdFast = 12;
    private const int MacdSlow = 26;
    private const int MacdSignalPeriod = 9;
    private const int BbPeriod = 20;
    private const decimal BbStdDevMultiplier = 2m;
    private const int Week52Period = 252;

    public static IndicatorRow Compute(Guid stockId, DateOnly date, IReadOnlyList<decimal> closePrices)
    {
        (decimal? macdLine, decimal? macdSignal, decimal? macdHistogram) = ComputeMacd(closePrices);
        (decimal? bbUpper, decimal? bbMid, decimal? bbLower) = ComputeBollingerBands(closePrices);
        (decimal? week52High, decimal? week52Low) = ComputeWeek52HighLow(closePrices);

        return new IndicatorRow(
            stockId,
            date,
            ComputeRsi(closePrices),
            macdLine,
            macdSignal,
            macdHistogram,
            ComputeSma(closePrices, 50),
            ComputeSma(closePrices, 200),
            bbUpper,
            bbMid,
            bbLower,
            week52High,
            week52Low
        );
    }

    public static decimal? ComputeRsi(IReadOnlyList<decimal> prices)
    {
        if (prices.Count < RsiPeriod + 1)
            return null;

        decimal avgGain = 0m;
        decimal avgLoss = 0m;

        for (int i = 1; i <= RsiPeriod; i++)
        {
            decimal change = prices[i] - prices[i - 1];
            if (change > 0)
                avgGain += change;
            else
                avgLoss += Math.Abs(change);
        }

        avgGain /= RsiPeriod;
        avgLoss /= RsiPeriod;

        for (int i = RsiPeriod + 1; i < prices.Count; i++)
        {
            decimal change = prices[i] - prices[i - 1];
            decimal gain = change > 0 ? change : 0m;
            decimal loss = change < 0 ? Math.Abs(change) : 0m;
            avgGain = (avgGain * (RsiPeriod - 1) + gain) / RsiPeriod;
            avgLoss = (avgLoss * (RsiPeriod - 1) + loss) / RsiPeriod;
        }

        if (avgLoss == 0m)
            return 100m;

        decimal rs = avgGain / avgLoss;
        return Math.Round(100m - (100m / (1m + rs)), 8);
    }

    public static (decimal? Line, decimal? Signal, decimal? Histogram) ComputeMacd(IReadOnlyList<decimal> prices)
    {
        if (prices.Count < MacdSlow)
            return (null, null, null);

        decimal emaFast = ComputeEmaRaw(prices, MacdFast);
        decimal emaSlow = ComputeEmaRaw(prices, MacdSlow);
        decimal macdLine = emaFast - emaSlow;

        if (prices.Count < MacdSlow + MacdSignalPeriod - 1)
            return (macdLine, null, null);

        List<decimal> macdSeries = BuildMacdSeries(prices);
        decimal signalLine = ComputeEmaRaw(macdSeries, MacdSignalPeriod);
        decimal histogram = macdLine - signalLine;

        return (macdLine, signalLine, histogram);
    }

    public static decimal? ComputeSma(IReadOnlyList<decimal> prices, int period)
    {
        if (prices.Count < period)
            return null;

        decimal sum = 0m;
        int start = prices.Count - period;
        for (int i = start; i < prices.Count; i++)
            sum += prices[i];

        return sum / period;
    }

    public static (decimal? Upper, decimal? Mid, decimal? Lower) ComputeBollingerBands(IReadOnlyList<decimal> prices)
    {
        if (prices.Count < BbPeriod)
            return (null, null, null);

        decimal sma = ComputeSma(prices, BbPeriod)!.Value;
        int start = prices.Count - BbPeriod;
        decimal variance = 0m;
        for (int i = start; i < prices.Count; i++)
        {
            decimal diff = prices[i] - sma;
            variance += diff * diff;
        }

        decimal stdDev = (decimal)Math.Sqrt((double)(variance / BbPeriod));
        decimal upper = sma + BbStdDevMultiplier * stdDev;
        decimal lower = sma - BbStdDevMultiplier * stdDev;

        return (upper, sma, lower);
    }

    public static (decimal? High, decimal? Low) ComputeWeek52HighLow(IReadOnlyList<decimal> prices)
    {
        if (prices.Count == 0)
            return (null, null);

        int start = Math.Max(0, prices.Count - Week52Period);
        decimal high = prices[start];
        decimal low = prices[start];

        for (int i = start + 1; i < prices.Count; i++)
        {
            if (prices[i] > high) high = prices[i];
            if (prices[i] < low) low = prices[i];
        }

        return (high, low);
    }

    private static decimal ComputeEmaRaw(IReadOnlyList<decimal> prices, int period)
    {
        decimal multiplier = 2m / (period + 1);
        decimal ema = prices[0];
        for (int i = 1; i < prices.Count; i++)
            ema = prices[i] * multiplier + ema * (1m - multiplier);
        return ema;
    }

    private static List<decimal> BuildMacdSeries(IReadOnlyList<decimal> prices)
    {
        decimal fastMult = 2m / (MacdFast + 1);
        decimal slowMult = 2m / (MacdSlow + 1);

        decimal emaFast = prices[0];
        decimal emaSlow = prices[0];
        List<decimal> macdSeries = [];

        for (int i = 1; i < prices.Count; i++)
        {
            emaFast = prices[i] * fastMult + emaFast * (1m - fastMult);
            emaSlow = prices[i] * slowMult + emaSlow * (1m - slowMult);
            if (i >= MacdSlow - 1)
                macdSeries.Add(emaFast - emaSlow);
        }

        return macdSeries;
    }
}
