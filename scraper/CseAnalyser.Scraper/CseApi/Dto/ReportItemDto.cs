using System.Text.Json.Serialization;

namespace CseAnalyser.Scraper.CseApi.Dto;

public record ReportItemDto(
    [property: JsonPropertyName("id")] int CseReportId,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("manualDate")] long ManualDateMs,
    [property: JsonPropertyName("uploadedDate")] long UploadedDateMs,
    [property: JsonPropertyName("fileText")] string FileText,
    [property: JsonPropertyName("path2")] string? Path2,
    [property: JsonPropertyName("authorizedDate")] long? AuthorizedDateMs
);
