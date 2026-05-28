# Soft delete Watchlist with Backfill on reactivation

Removing a Stock from the Watchlist sets it **Inactive** rather than deleting its data. The scraper skips Inactive Stocks and the UI hides them, but all price history, reports, indicators, annotations, and recommendations are preserved. When a Stock is reactivated, a **Backfill** is triggered to recover any missing price data (up to ~1 year via the CSE `period=5` API limit) and any reports published during the inactive period (full history available regardless of gap length).

Hard delete was rejected because a user may have annotated report figures, flagged one-off events, and accumulated recommendation history — losing that context because a stock was temporarily removed from the watchlist would silently corrupt the decision-support record. Full collection of all ~300 CSE stocks was rejected as it would exceed the Supabase 500MB free tier.

## Consequences

Price history gaps older than 1 year from a reactivation are permanent — the CSE API does not provide data beyond ~1 year via `companyChartDataByStock`.
