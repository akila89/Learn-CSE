namespace CseAnalyser.Scraper.CseApi;

public record CseApiResult<T>
{
    public CseApiError? Error { get; init; }
    public bool IsSuccess => Error is null;
    public T? Value { get; init; }

    public static CseApiResult<T> Failure(CseApiError error) => new() { Error = error };
    public static CseApiResult<T> Success(T value) => new() { Value = value };
}
