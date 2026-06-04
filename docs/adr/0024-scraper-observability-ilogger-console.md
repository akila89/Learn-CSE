# Scraper Observability — ILogger with Console Provider

The scraper runs as a nightly GitHub Actions job with no external logging infrastructure. We use `Microsoft.Extensions.Logging` with the Console provider rather than `Console.WriteLine` so that log levels (`Information`, `Warning`, `Error`) are first-class — errors stand out visually in the Actions UI, and verbose per-stock output can be enabled on demand via `LOGGING__LOGLEVEL__DEFAULT=Debug` without a code change.

## Considered Options

**`Console.WriteLine` over `ILogger`** — zero setup, but no log levels. A failed stock and a normal progress line look identical. Ruled out because triage of a failed nightly run depends on errors being visually distinct.

**Structured JSON over plain text** — future-proof for log aggregation tools, but there is no such tooling planned (see RESEARCH.md). JSON is unreadable raw in the Actions UI. Plain text chosen until an aggregation tool is introduced.

## Key decisions

- **Plain text** format — readable directly in the GitHub Actions run log.
- **Short explicit category names** (`"PriceScraper"`, `"HolidayDetection"`) passed via `loggerFactory.CreateLogger(name)` rather than the default fully-qualified class name from `ILogger<T>`. The FQCN adds noise with no filtering benefit in a single-concern app.
- **`Information` default** in `appsettings.json`; `Debug` enabled via `LOGGING__LOGLEVEL__DEFAULT` env var. Per-stock success lines are logged at `Debug` — hidden in normal runs, visible when diagnosing a specific failure.
- **Constructor injection** — each phase accepts its logger as a constructor parameter. Tests use `NullLogger<T>.Instance`.
- **No logger on `ScraperPipeline`** — phases own their own output; the run boundary is covered by `ScraperRunLoggerPhase`.
