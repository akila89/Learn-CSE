using System.Net;
using System.Text;
using CseAnalyser.Scraper.CseApi;
using CseAnalyser.Scraper.CseApi.Dto;

namespace CseAnalyser.Scraper.Tests.CseApi;

public class CseApiClientTests
{
    [Fact]
    public async Task GetAllSecurityCodesAsync_returns_mapped_chart_ids()
    {
        string json = """[{"id":297,"symbol":"JKH.N0000","name":"John Keells Holdings PLC"}]""";
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.OK, json);

        CseApiResult<IReadOnlyList<SecurityCodeDto>> result = await client.GetAllSecurityCodesAsync();

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(297, result.Value![0].CseChartId);
        Assert.Equal("JKH.N0000", result.Value![0].Symbol);
    }

    [Fact]
    public async Task GetAllSecurityCodesAsync_uses_GET()
    {
        (CseApiClient client, CapturingHandler handler) = Build(HttpStatusCode.OK, "[]");

        await client.GetAllSecurityCodesAsync();

        Assert.Equal(HttpMethod.Get, handler.LastRequest!.Method);
    }

    [Fact]
    public async Task GetChartDataAsync_maps_ohlcv_fields_correctly()
    {
        string json = """
            {"chartData":[{"h":200.5,"l":195.0,"o":null,"p":198.75,"q":12500,"t":1748649600000,"c":null,"pc":null}]}
            """;
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.OK, json);

        CseApiResult<IReadOnlyList<OhlcvDataPointDto>> result = await client.GetChartDataAsync(cseChartId: 297);

        Assert.True(result.IsSuccess);
        OhlcvDataPointDto pt = result.Value![0];
        Assert.Equal(200.5m, pt.High);
        Assert.Equal(195.0m, pt.Low);
        Assert.Null(pt.Open);
        Assert.Equal(198.75m, pt.Close);
        Assert.Equal(12500L, pt.Volume);
        Assert.Equal(1748649600000L, pt.TimestampMs);
        Assert.Null(pt.Change);
        Assert.Null(pt.PercentChange);
    }

    [Fact]
    public async Task GetChartDataAsync_sends_chart_id_not_security_id()
    {
        (CseApiClient client, CapturingHandler handler) = Build(HttpStatusCode.OK, """{"chartData":[]}""");

        await client.GetChartDataAsync(cseChartId: 297);

        string body = await handler.LastRequest!.Content!.ReadAsStringAsync();
        Assert.Contains("stockId=297", body);
    }

    [Fact]
    public async Task GetChartDataAsync_uses_form_urlencoded_content_type()
    {
        (CseApiClient client, CapturingHandler handler) = Build(HttpStatusCode.OK, """{"chartData":[]}""");

        await client.GetChartDataAsync(cseChartId: 297);

        Assert.Equal("application/x-www-form-urlencoded",
            handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task GetCompanyInfoAsync_extracts_cse_security_id()
    {
        string json = CompanyInfoJson(securityId: 508, symbol: "JKH.N0000", name: "JOHN KEELLS HOLDINGS PLC");
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.OK, json);

        CseApiResult<CompanyInfoDto> result = await client.GetCompanyInfoAsync("JKH.N0000");

        Assert.True(result.IsSuccess);
        Assert.Equal(508, result.Value!.CseSecurityId);
    }

    [Fact]
    public async Task GetCompanyInfoAsync_security_id_differs_from_chart_id()
    {
        // JKH chart id = 297, security id = 508 — must not be interchangeable
        string json = CompanyInfoJson(securityId: 508, symbol: "JKH.N0000", name: "JOHN KEELLS HOLDINGS PLC");
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.OK, json);

        CseApiResult<CompanyInfoDto> result = await client.GetCompanyInfoAsync("JKH.N0000");

        Assert.True(result.IsSuccess);
        Assert.NotEqual(297, result.Value!.CseSecurityId);
        Assert.Equal(508, result.Value!.CseSecurityId);
    }

    [Fact]
    public async Task GetCompanyInfoAsync_uses_multipart_form_data()
    {
        string json = CompanyInfoJson(securityId: 1, symbol: "X", name: "X");
        (CseApiClient client, CapturingHandler handler) = Build(HttpStatusCode.OK, json);

        await client.GetCompanyInfoAsync("JKH.N0000");

        Assert.StartsWith("multipart/form-data",
            handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task GetFinancialsAsync_maps_annual_and_quarterly_reports()
    {
        string json = """
            {
              "infoAnnualData": [
                {"id":51321,"path":"cmt/upload_report_file/508_1779789508055.pdf",
                 "manualDate":1779733800000,"uploadedDate":1779789508055,
                 "fileText":"Annual Report as at 31st March 2026","path2":null,
                 "authorizedDate":1779794879336}
              ],
              "infoQuarterlyData": [
                {"id":49001,"path":"cmt/upload_report_file/508_q1.pdf",
                 "manualDate":1748304000000,"uploadedDate":1748350000000,
                 "fileText":"Q1 2026","path2":null,"authorizedDate":null}
              ]
            }
            """;
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.OK, json);

        CseApiResult<FinancialsDto> result = await client.GetFinancialsAsync("JKH.N0000");

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.AnnualReports);
        Assert.Equal(51321, result.Value!.AnnualReports[0].CseReportId);
        Assert.Single(result.Value!.QuarterlyReports);
        Assert.Equal(49001, result.Value!.QuarterlyReports[0].CseReportId);
    }

    [Fact]
    public async Task GetFinancialsAsync_uses_form_urlencoded_content_type()
    {
        string json = """{"infoAnnualData":[],"infoQuarterlyData":[]}""";
        (CseApiClient client, CapturingHandler handler) = Build(HttpStatusCode.OK, json);

        await client.GetFinancialsAsync("JKH.N0000");

        Assert.Equal("application/x-www-form-urlencoded",
            handler.LastRequest!.Content!.Headers.ContentType!.MediaType);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task non_200_responses_return_typed_error_not_exception(HttpStatusCode status)
    {
        (CseApiClient client, CapturingHandler _) = Build(status, "error body");

        CseApiResult<IReadOnlyList<SecurityCodeDto>> result = await client.GetAllSecurityCodesAsync();

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Equal((int)status, result.Error!.StatusCode);
    }

    [Fact]
    public async Task non_200_response_includes_response_body_in_error()
    {
        (CseApiClient client, CapturingHandler _) = Build(HttpStatusCode.BadRequest, "Bad request detail");

        CseApiResult<IReadOnlyList<SecurityCodeDto>> result = await client.GetAllSecurityCodesAsync();

        Assert.Equal("Bad request detail", result.Error!.Message);
    }

    private static (CseApiClient client, CapturingHandler handler) Build(
        HttpStatusCode status,
        string responseBody)
    {
        CapturingHandler handler = new(status, responseBody);
        HttpClient httpClient = new(handler);
        return (new CseApiClient(httpClient), handler);
    }

    private static string CompanyInfoJson(int securityId, string symbol, string name) => $$"""
        {
          "reqSymbolBetaInfo": {"securityId": {{securityId}}},
          "reqSymbolInfo": {"symbol": "{{symbol}}", "name": "{{name}}"}
        }
        """;
}

internal sealed class CapturingHandler(HttpStatusCode status, string body) : HttpMessageHandler
{
    public HttpRequestMessage? LastRequest { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        LastRequest = request;
        HttpResponseMessage response = new(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
