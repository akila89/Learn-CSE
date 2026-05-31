using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.CseApi.Dto;
using Xunit.Abstractions;

namespace CseAnalyser.Scraper.Tests.CseApi;

/// <summary>
/// Hits the live CSE API. Run manually to verify DTO field mappings.
/// Skip in CI: dotnet test --filter "Category!=Integration"
/// </summary>
[Trait("Category", "Integration")]
public class CseApiClientIntegrationTests(ITestOutputHelper output)
{
    [Fact]
    public async Task GetAllSecurityCodes_returns_non_empty_list_with_valid_ids()
    {
        CseApiResult<IReadOnlyList<SecurityCodeDto>> result = await BuildLiveClient().GetAllSecurityCodesAsync();

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.NotEmpty(result.Value!);

        SecurityCodeDto first = result.Value![0];
        Assert.True(first.CseChartId > 0, $"Expected CseChartId > 0, got {first.CseChartId}. Check JsonPropertyName on SecurityCodeDto.");
        Assert.False(string.IsNullOrEmpty(first.Symbol), "Symbol is empty — check JsonPropertyName.");
        Assert.False(string.IsNullOrEmpty(first.Name), "Name is empty — check JsonPropertyName.");

        output.WriteLine($"Sample stock — CseChartId:{first.CseChartId} Symbol:{first.Symbol} Name:{first.Name}");
        output.WriteLine($"Total stocks returned: {result.Value!.Count}");
    }

    [Fact]
    public async Task GetChartData_for_JKH_returns_ohlcv_data()
    {
        const int JkhChartId = 297;

        CseApiResult<IReadOnlyList<OhlcvDataPointDto>> result = await BuildLiveClient().GetChartDataAsync(JkhChartId, period: 5);

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.NotEmpty(result.Value!);

        OhlcvDataPointDto pt = result.Value![0];
        Assert.True(pt.High > 0, $"High is {pt.High} — check JsonPropertyName 'h' on OhlcvDataPointDto.");
        Assert.True(pt.Close > 0, $"Close is {pt.Close} — check JsonPropertyName 'p'.");
        Assert.True(pt.Volume > 0, $"Volume is {pt.Volume} — check JsonPropertyName 'q'.");
        Assert.True(pt.TimestampMs > 0, $"TimestampMs is {pt.TimestampMs} — check JsonPropertyName 't'.");
        // 'o' (Open), 'l' (Low), 'c' (Change), 'pc' (PercentChange) are nullable — null in many real rows

        output.WriteLine($"Sample OHLCV — H:{pt.High} L:{pt.Low} O:{pt.Open} C:{pt.Close} V:{pt.Volume} T:{pt.TimestampMs}");
        output.WriteLine($"Total data points: {result.Value!.Count}");
    }

    [Fact]
    public async Task GetCompanyInfo_for_JKH_returns_security_id()
    {
        CseApiResult<CompanyInfoDto> result = await BuildLiveClient().GetCompanyInfoAsync("JKH.N0000");

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.True(result.Value!.CseSecurityId > 0,
            $"CseSecurityId is {result.Value!.CseSecurityId} — check JsonPropertyName 'securityId' on CompanyInfoDto.");
        Assert.False(string.IsNullOrEmpty(result.Value!.Symbol), "Symbol is empty — check JsonPropertyName.");

        output.WriteLine($"JKH — CseSecurityId:{result.Value!.CseSecurityId} Symbol:{result.Value!.Symbol} Company:{result.Value!.CompanyName}");
    }

    [Fact]
    public async Task GetChartData_for_COMB_X0000_returns_ohlcv_data()
    {
        const int CombXChartId = 396;

        CseApiResult<IReadOnlyList<OhlcvDataPointDto>> result = await BuildLiveClient().GetChartDataAsync(CombXChartId, period: 5);

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.NotEmpty(result.Value!);
        Assert.True(result.Value![0].Close > 0, "Close is 0 — non-voting share price data missing.");

        output.WriteLine($"COMB.X0000 data points: {result.Value!.Count}, latest close: {result.Value![0].Close}");
    }

    [Fact]
    public async Task GetCompanyInfo_for_COMB_X0000_returns_security_id()
    {
        CseApiResult<CompanyInfoDto> result = await BuildLiveClient().GetCompanyInfoAsync("COMB.X0000");

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.True(result.Value!.CseSecurityId > 0, "CseSecurityId is 0 for non-voting share.");
        Assert.False(string.IsNullOrEmpty(result.Value!.Symbol), "Symbol is empty.");

        output.WriteLine($"COMB.X0000 — CseSecurityId:{result.Value!.CseSecurityId} Symbol:{result.Value!.Symbol}");
    }

    [Fact]
    public async Task GetFinancials_for_COMB_X0000_returns_reports()
    {
        CseApiResult<FinancialsDto> result = await BuildLiveClient().GetFinancialsAsync("COMB.X0000");

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.NotEmpty(result.Value!.AnnualReports);

        output.WriteLine($"COMB.X0000 annual reports: {result.Value!.AnnualReports.Count}, quarterly: {result.Value!.QuarterlyReports.Count}");
    }

    [Fact]
    public async Task GetFinancials_for_JKH_returns_annual_and_quarterly_reports()
    {
        CseApiResult<FinancialsDto> result = await BuildLiveClient().GetFinancialsAsync("JKH.N0000");

        Assert.True(result.IsSuccess, $"API error: {result.Error?.StatusCode} {result.Error?.Message}");
        Assert.NotEmpty(result.Value!.AnnualReports);
        Assert.NotEmpty(result.Value!.QuarterlyReports);

        ReportItemDto annual = result.Value!.AnnualReports[0];
        Assert.True(annual.CseReportId > 0, $"CseReportId is {annual.CseReportId} — check JsonPropertyName 'id' on ReportItemDto.");
        Assert.False(string.IsNullOrEmpty(annual.Path), "Path is empty — check JsonPropertyName 'path'.");
        Assert.True(annual.ManualDateMs > 0, $"ManualDateMs is {annual.ManualDateMs} — check JsonPropertyName 'manualDate'.");

        output.WriteLine($"Annual reports: {result.Value!.AnnualReports.Count}");
        output.WriteLine($"Sample annual — Id:{annual.CseReportId} Path:{annual.Path} FileText:{annual.FileText}");
        output.WriteLine($"Quarterly reports: {result.Value!.QuarterlyReports.Count}");
    }

    private CseApiClient BuildLiveClient() => new(new HttpClient());
}
