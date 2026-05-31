using System.Text.Json.Serialization;

namespace CseAnalyser.Scraper.CseApi.Dto;

public record SecurityCodeDto(
    [property: JsonPropertyName("id")] int CseChartId,
    [property: JsonPropertyName("symbol")] string Symbol,
    [property: JsonPropertyName("name")] string Name
);
