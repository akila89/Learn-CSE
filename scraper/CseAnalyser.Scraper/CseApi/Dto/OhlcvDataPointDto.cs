using System.Text.Json.Serialization;

namespace CseAnalyser.Scraper.CseApi.Dto;

public record OhlcvDataPointDto(
    [property: JsonPropertyName("h")] decimal? High,
    [property: JsonPropertyName("l")] decimal? Low,
    [property: JsonPropertyName("o")] decimal? Open,
    [property: JsonPropertyName("p")] decimal Close,
    [property: JsonPropertyName("q")] long Volume,
    [property: JsonPropertyName("t")] long TimestampMs,
    [property: JsonPropertyName("c")] decimal? Change,
    [property: JsonPropertyName("pc")] decimal? PercentChange
);
