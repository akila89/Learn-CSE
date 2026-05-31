using System.Text.Json;
using System.Text.Json.Serialization;
using CseAnalyser.Scraper.CseApi.Dto;

namespace CseAnalyser.Scraper.CseApi;

public class CseApiClient(HttpClient httpClient)
{
    private const string BaseUrl = "https://www.cse.lk/api/";

    public async Task<CseApiResult<IReadOnlyList<SecurityCodeDto>>> GetAllSecurityCodesAsync(
        CancellationToken ct = default)
    {
        HttpResponseMessage response = await httpClient.GetAsync($"{BaseUrl}allSecurityCode", ct);
        return await ParseResponse<IReadOnlyList<SecurityCodeDto>>(response);
    }

    public async Task<CseApiResult<IReadOnlyList<OhlcvDataPointDto>>> GetChartDataAsync(
        int cseChartId,
        int period = 1, // years of OHLCV history; CSE API accepts 1–5
        CancellationToken ct = default)
    {
        FormUrlEncodedContent content = new([
            new KeyValuePair<string, string>("stockId", cseChartId.ToString()),
            new KeyValuePair<string, string>("period", period.ToString()),
        ]);
        HttpResponseMessage response = await httpClient.PostAsync($"{BaseUrl}companyChartDataByStock", content, ct);
        CseApiResult<ChartDataResponse> wrapped = await ParseResponse<ChartDataResponse>(response);
        if (!wrapped.IsSuccess)
        {
            return CseApiResult<IReadOnlyList<OhlcvDataPointDto>>.Failure(wrapped.Error!);
        }

        return CseApiResult<IReadOnlyList<OhlcvDataPointDto>>.Success(wrapped.Value!.ChartData);
    }

    public async Task<CseApiResult<CompanyInfoDto>> GetCompanyInfoAsync(
        string symbol,
        CancellationToken ct = default)
    {
        MultipartFormDataContent content = new();
        content.Add(new StringContent(symbol), "symbol");
        HttpResponseMessage response = await httpClient.PostAsync($"{BaseUrl}companyInfoSummery", content, ct);
        CseApiResult<CompanyInfoResponse> raw = await ParseResponse<CompanyInfoResponse>(response);
        if (!raw.IsSuccess)
        {
            return CseApiResult<CompanyInfoDto>.Failure(raw.Error!);
        }

        CompanyInfoDto dto = new(
            raw.Value!.BetaInfo.CseSecurityId,
            raw.Value!.SymbolInfo.Symbol,
            raw.Value!.SymbolInfo.Name
        );
        return CseApiResult<CompanyInfoDto>.Success(dto);
    }

    public async Task<CseApiResult<FinancialsDto>> GetFinancialsAsync(
        string symbol,
        CancellationToken ct = default)
    {
        FormUrlEncodedContent content = new([
            new KeyValuePair<string, string>("symbol", symbol),
        ]);
        HttpResponseMessage response = await httpClient.PostAsync($"{BaseUrl}financials", content, ct);
        return await ParseResponse<FinancialsDto>(response);
    }

    private static async Task<CseApiResult<T>> ParseResponse<T>(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            string body = await response.Content.ReadAsStringAsync();
            return CseApiResult<T>.Failure(new CseApiError((int)response.StatusCode, body));
        }

        string json = await response.Content.ReadAsStringAsync();
        T? result = JsonSerializer.Deserialize<T>(json);
        if (result is null)
        {
            return CseApiResult<T>.Failure(new CseApiError((int)response.StatusCode, "Response body was empty or could not be deserialized."));
        }

        return CseApiResult<T>.Success(result);
    }
}

// Raw API response shapes — internal to this file, not exposed to callers.
file record ChartDataResponse(
    [property: JsonPropertyName("chartData")] IReadOnlyList<OhlcvDataPointDto> ChartData
);

file record CompanyInfoResponse(
    [property: JsonPropertyName("reqSymbolBetaInfo")] BetaInfoSection BetaInfo,
    [property: JsonPropertyName("reqSymbolInfo")] SymbolInfoSection SymbolInfo
);

file record BetaInfoSection(
    [property: JsonPropertyName("securityId")] int CseSecurityId
);

file record SymbolInfoSection(
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("name")] string Name
);
