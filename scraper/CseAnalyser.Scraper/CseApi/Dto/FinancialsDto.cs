using System.Text.Json.Serialization;

namespace CseAnalyser.Scraper.CseApi.Dto;

public record FinancialsDto(
    [property: JsonPropertyName("infoAnnualData")] IReadOnlyList<ReportItemDto> AnnualReports,
    [property: JsonPropertyName("infoQuarterlyData")] IReadOnlyList<ReportItemDto> QuarterlyReports
);
