namespace CseAnalyser.Scraper.CseApi.Dto;

public record CompanyInfoDto(
    int CseSecurityId,
    string Symbol,
    string CompanyName
);
