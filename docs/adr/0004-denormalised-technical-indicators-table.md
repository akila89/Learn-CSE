# Denormalised technical indicators table

Technical Indicators are stored in a single `daily_indicators` table with one row per Stock per day and flat columns for each indicator (RSI, MACD line, MACD signal, SMA 50, SMA 200, Bollinger upper/mid/lower, 52-week high/low). New indicators are added as new columns via `ALTER TABLE`.

The normalised alternative — an EAV table `(stock_id, date, indicator_name, value)` — was rejected because: (1) querying a full day's indicators requires a pivot or multiple rows per fetch; (2) the Supabase auto-REST API returns flat rows natively, making the denormalised shape directly usable in Angular without transformation; (3) the set of technical indicators is known and bounded — the flexibility of EAV is not needed. Adding a new indicator is a single `ALTER TABLE ADD COLUMN`, which is no more complex than inserting a new `indicator_name` value into an EAV table.
