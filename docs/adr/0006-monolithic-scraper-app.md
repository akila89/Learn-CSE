# Single monolithic .NET scraper app

All scraper phases — price fetching, holiday detection, indicator computation, report detection, report ingestion, and recommendation generation — run as sequential phases inside one .NET console app, triggered by one GitHub Actions workflow. A split-phase approach (separate apps or workflows per concern) was rejected because: report detection and price scraping share the same nightly cadence so there is no scheduling benefit to separating them; cross-workflow dependencies add coordination complexity; and each GitHub Actions job has a startup cost that compounds with more jobs. Internally, phases are clearly separated as distinct classes and are independently testable.

## Phase order

1. Refresh `cse_all_stocks` cache if older than 7 days (weekly, via `allSecurityCode` upsert)
2. Load Active Stocks from Supabase
3. Scrape prices → `daily_prices`
4. Detect market holiday (compare latest returned price date vs stored)
5. If no new price data → skip indicators and recommendations; **still run report detection**
6. Compute Technical Indicators → `daily_indicators`
7. Detect and ingest new Reports → fundamentals tables (see ADRs 0021, 0022, 0023 for detection key, failure handling, and period labelling)
8. Generate Recommendations → `recommendations`
9. Write `scraper_runs` log row

Report detection (step 7) is never skipped — a company can publish a quarterly report on a public holiday. Only indicator computation and recommendation generation are skipped when no new price data is detected.

Report detection reads both `infoAnnualData` and `infoQuarterlyData` from the CSE `financials` endpoint. `infoOtherData` (press releases, prospectuses, errata) is ignored.

## First-time vs incremental

Any Active Stock with no rows in `daily_prices` is treated as a new stock and runs a full historical load (1 year of prices via `period=5`, all historical reports via `financials`) before the normal incremental phase. The same path handles reactivated stocks with a price gap. This detection happens at startup before the main processing loop.
