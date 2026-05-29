-- CSE Stock Analyser — Reference Schema
-- All tables require RLS policies: SELECT/INSERT/UPDATE where auth.uid() matches the app user.

-- ─── Core entities ────────────────────────────────────────────────────────────

CREATE TABLE companies (
  id             UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name           TEXT NOT NULL UNIQUE,
  fy_end_month   INTEGER CHECK (fy_end_month BETWEEN 1 AND 12),  -- financial year end month; auto-set from first annual report manualDate; null until first annual ingested
  profile        JSONB,  -- Company Profile: sector, business description, revenue characteristics
  insights       JSONB,  -- Auto-generated Insights from Gemini, refreshed on each report ingestion
  created_at     TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE stocks (
  id                UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  company_id        UUID NOT NULL REFERENCES companies(id),
  symbol            TEXT NOT NULL UNIQUE,  -- e.g. JKH.N0000
  cse_chart_id      INTEGER NOT NULL,      -- from allSecurityCode; used for price/chart API calls
  cse_security_id      INTEGER NOT NULL,      -- from companyInfoSummery; used for report/news API calls
  last_price_scraped_at TIMESTAMPTZ,          -- updated by scraper each run; used for Stale detection
  created_at           TIMESTAMPTZ DEFAULT now()
);

-- ─── Watchlist ────────────────────────────────────────────────────────────────

CREATE TABLE watchlist_entries (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id     UUID NOT NULL REFERENCES auth.users(id),
  stock_id    UUID NOT NULL REFERENCES stocks(id),
  is_active   BOOLEAN NOT NULL DEFAULT true,
  stock_note  TEXT,         -- freeform per-user per-stock observation (Stock Note)
  added_at    TIMESTAMPTZ DEFAULT now(),
  removed_at  TIMESTAMPTZ,
  UNIQUE (user_id, stock_id)
);

-- Scraper query: SELECT DISTINCT stock_id FROM watchlist_entries WHERE is_active = true

-- ─── Price data ───────────────────────────────────────────────────────────────

CREATE TABLE daily_prices (
  id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id    UUID NOT NULL REFERENCES stocks(id),
  date        DATE NOT NULL,
  open        NUMERIC,
  high        NUMERIC,
  low         NUMERIC,
  close       NUMERIC,
  volume      BIGINT,
  change      NUMERIC,
  change_pct  NUMERIC,
  UNIQUE (stock_id, date)
);

-- ─── Technical indicators (denormalised — ADR 0004) ───────────────────────────

CREATE TABLE daily_indicators (
  id           UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id     UUID NOT NULL REFERENCES stocks(id),
  date         DATE NOT NULL,
  rsi          NUMERIC,
  macd_line    NUMERIC,
  macd_signal  NUMERIC,
  sma_50       NUMERIC,
  sma_200      NUMERIC,
  bb_upper     NUMERIC,
  bb_mid       NUMERIC,
  bb_lower     NUMERIC,
  week52_high      NUMERIC,
  week52_low       NUMERIC,
  p_e_ratio        NUMERIC,  -- close / EPS; requires stock_fundamentals to be populated
  dividend_yield   NUMERIC,  -- DPS / close; requires stock_fundamentals to be populated
  -- new indicators added here via ALTER TABLE ADD COLUMN
  UNIQUE (stock_id, date)
);

-- ─── Reports and extraction ───────────────────────────────────────────────────

CREATE TABLE reports (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id         UUID NOT NULL REFERENCES stocks(id),
  cse_report_id    INTEGER NOT NULL UNIQUE,  -- CSE-assigned report ID from financials endpoint; dedup key
  report_type      TEXT NOT NULL CHECK (report_type IN ('annual', 'quarterly')),
  period           TEXT NOT NULL,   -- e.g. 'FY2024/25', 'Q3 FY2024/25'
  audit_status     TEXT NOT NULL CHECK (audit_status IN ('audited', 'unaudited')),
  ingestion_status TEXT NOT NULL DEFAULT 'download_failed' CHECK (ingestion_status IN ('download_failed', 'stored', 'confirmed')),
                                   -- download_failed: scraper retries each run until PDF fetched or user uploads manually
                                   -- stored: PDF in Supabase Storage; extraction may or may not have run (check raw_extraction)
                                   -- confirmed: fundamentals written to company_fundamentals + stock_fundamentals
  pdf_path         TEXT,           -- Supabase Storage path; null on stub rows (download_failed)
  cse_pdf_url      TEXT,            -- original CSE CDN URL
  raw_extraction   JSONB,           -- Gemini's original output; never modified
  extraction       JSONB,           -- working copy; auto-populated from raw; user-editable; includes field footnotes
  corrected_at     TIMESTAMPTZ,     -- set when user edits extraction; null if never touched
  is_latest        BOOLEAN NOT NULL DEFAULT true,  -- false when superseded by a corrected re-filing
  ingested_at      TIMESTAMPTZ DEFAULT now()
);

-- Only one latest report per stock per period; re-filings set old row is_latest=false
CREATE UNIQUE INDEX reports_stock_period_latest ON reports(stock_id, period) WHERE is_latest = true;

-- ─── Fundamentals ─────────────────────────────────────────────────────────────

-- Company-level figures (ADR 0001 — shared across share classes, keyed by period)
CREATE TABLE company_fundamentals (
  id                      UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  company_id              UUID NOT NULL REFERENCES companies(id),
  report_id               UUID NOT NULL REFERENCES reports(id),
  period                  TEXT NOT NULL,
  audit_status            TEXT NOT NULL,
  revenue                 NUMERIC,
  revenue_prior           NUMERIC,
  revenue_change_pct      NUMERIC,
  net_profit              NUMERIC,
  net_profit_prior        NUMERIC,
  net_profit_change_pct   NUMERIC,
  roe                     NUMERIC,
  roe_prior               NUMERIC,
  debt_equity             NUMERIC,
  debt_equity_prior       NUMERIC,
  nav                     NUMERIC,
  nav_prior               NUMERIC,
  extras                  JSONB,    -- additional_items from Extraction Schema outside fixed fields
  has_conflict            BOOLEAN DEFAULT false,  -- true when .N and .X reports disagree on figures
  conflict_detail         JSONB,    -- { field, n_value, x_value } for each conflicting figure
  UNIQUE (company_id, period)
);

-- Stock-level figures (EPS, DPS differ between .N and .X share classes)
CREATE TABLE stock_fundamentals (
  id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id         UUID NOT NULL REFERENCES stocks(id),
  report_id        UUID NOT NULL REFERENCES reports(id),
  period           TEXT NOT NULL,
  audit_status     TEXT NOT NULL,
  eps              NUMERIC,
  eps_prior        NUMERIC,
  eps_change_pct   NUMERIC,
  dps              NUMERIC,
  dps_prior        NUMERIC,
  extras           JSONB,
  UNIQUE (stock_id, period)
);

-- ─── Annotations (v2 — deferred) ─────────────────────────────────────────────

-- Field-level user annotations on extracted report figures are deferred to v2.
-- v1 Recommendations are generated without inline annotation context.
--
-- CREATE TABLE field_annotations (
--   id          UUID PRIMARY KEY DEFAULT gen_random_uuid(),
--   report_id   UUID NOT NULL REFERENCES reports(id),
--   field_name  TEXT NOT NULL,
--   user_id     UUID NOT NULL REFERENCES auth.users(id),
--   user_note   TEXT NOT NULL,
--   created_at  TIMESTAMPTZ DEFAULT now(),
--   UNIQUE (report_id, field_name, user_id)
-- );

-- ─── Recommendations ──────────────────────────────────────────────────────────

CREATE TABLE recommendations (
  id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id        UUID NOT NULL REFERENCES stocks(id),
  generated_at    TIMESTAMPTZ DEFAULT now(),
  signal          TEXT NOT NULL CHECK (signal IN ('BUY', 'ACCUMULATE', 'HOLD', 'REDUCE', 'SELL')),
  confidence_pct  INTEGER,
  reasoning       TEXT,
  risks           TEXT,
  prompt_sent     TEXT,   -- full Gemini prompt; stored for auditing and prompt iteration
  raw_response    TEXT    -- full Gemini response
);

-- ─── CSE stock list cache ─────────────────────────────────────────────────────

-- Read-only reference data — no RLS required, safe for anon key reads.
-- Refreshed weekly by the scraper (ADR 0013). Used by Stock Manager search.
CREATE TABLE cse_all_stocks (
  symbol        TEXT PRIMARY KEY,   -- e.g. JKH.N0000
  name          TEXT NOT NULL,
  cse_chart_id  INTEGER NOT NULL,
  refreshed_at  TIMESTAMPTZ DEFAULT now()
);

-- ─── Scraper ──────────────────────────────────────────────────────────────────

CREATE TABLE scraper_runs (
  id                    UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  run_at                TIMESTAMPTZ DEFAULT now(),
  status                TEXT NOT NULL CHECK (status IN ('success', 'partial', 'failed', 'holiday')),
  stocks_attempted      INTEGER,
  stocks_succeeded      INTEGER,
  stocks_failed         INTEGER,
  no_trading_detected   BOOLEAN DEFAULT false,  -- true = possible holiday, suppresses Stale badge
  error_detail          JSONB   -- array of { stock_id, symbol, phase, error_message } per failure
);
