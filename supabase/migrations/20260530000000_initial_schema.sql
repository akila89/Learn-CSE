-- CSE Stock Analyser — v1 initial schema
-- Apply with: supabase db push
--
-- field_annotations intentionally absent — deferred to v2 (scope decision in v1_scope memory)

-- ============================================================
-- 1. companies
-- ============================================================
CREATE TABLE companies (
  id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  name          TEXT        NOT NULL,
  symbol_root   TEXT        NOT NULL UNIQUE,  -- e.g. "JKH", "COMB"
  fy_end_month  SMALLINT    CHECK (fy_end_month BETWEEN 1 AND 12),  -- auto-derived from first annual report
  profile       TEXT,                          -- Company Profile (Gemini-extracted from latest annual report)
  insights      JSONB,                         -- auto-generated Insight list, refreshed on each report ingestion
  created_at    TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE companies ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON companies
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_companies_symbol_root ON companies (symbol_root);

-- ============================================================
-- 2. stocks
-- ============================================================
CREATE TABLE stocks (
  id                    UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  company_id            UUID        NOT NULL REFERENCES companies(id),
  symbol                TEXT        NOT NULL UNIQUE,  -- e.g. "JKH.N0000"
  cse_chart_id          INTEGER,                      -- from allSecurityCode; used for companyChartDataByStock
  cse_security_id       INTEGER,                      -- from companyInfoSummery; used for news/report endpoints
  last_price_scraped_at TIMESTAMPTZ,
  created_at            TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE stocks ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON stocks
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_stocks_company_id ON stocks (company_id);

-- ============================================================
-- 3. watchlist_entries — soft delete (ADR 0002)
-- ============================================================
CREATE TABLE watchlist_entries (
  id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id       UUID        NOT NULL REFERENCES stocks(id) UNIQUE,
  is_active      BOOLEAN     NOT NULL DEFAULT true,
  added_at       TIMESTAMPTZ NOT NULL DEFAULT now(),
  deactivated_at TIMESTAMPTZ
);

ALTER TABLE watchlist_entries ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON watchlist_entries
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_watchlist_entries_is_active ON watchlist_entries (is_active);

-- ============================================================
-- 4. scraper_runs
-- ============================================================
CREATE TABLE scraper_runs (
  id               UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  run_at           TIMESTAMPTZ NOT NULL DEFAULT now(),
  status           TEXT        NOT NULL CHECK (status IN ('success', 'partial', 'failed')),
  stocks_attempted INTEGER,
  stocks_failed    INTEGER,
  error_detail     TEXT
);

ALTER TABLE scraper_runs ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON scraper_runs
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_scraper_runs_run_at ON scraper_runs (run_at DESC);

-- ============================================================
-- 5. cse_all_stocks — no RLS; public reference data (ADR 0013)
-- ============================================================
CREATE TABLE cse_all_stocks (
  id           INTEGER     PRIMARY KEY,  -- CSE numeric id from allSecurityCode
  symbol       TEXT        NOT NULL,
  name         TEXT        NOT NULL,
  refreshed_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- ============================================================
-- 6. daily_prices
-- ============================================================
CREATE TABLE daily_prices (
  id         UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id   UUID    NOT NULL REFERENCES stocks(id),
  date       DATE    NOT NULL,
  open       NUMERIC,
  high       NUMERIC,
  low        NUMERIC,
  close      NUMERIC NOT NULL,
  volume     BIGINT,
  change     NUMERIC,
  change_pct NUMERIC,
  UNIQUE (stock_id, date)
);

ALTER TABLE daily_prices ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON daily_prices
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_daily_prices_stock_date ON daily_prices (stock_id, date DESC);

-- ============================================================
-- 7. daily_indicators — denormalised, one row per stock per day (ADR 0004)
--    New indicators added as ALTER TABLE ADD COLUMN — no redesign needed
-- ============================================================
CREATE TABLE daily_indicators (
  id             UUID    PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id       UUID    NOT NULL REFERENCES stocks(id),
  date           DATE    NOT NULL,
  rsi_14         NUMERIC,
  macd_line      NUMERIC,
  macd_signal    NUMERIC,
  macd_histogram NUMERIC,
  sma_50         NUMERIC,
  sma_200        NUMERIC,
  bb_upper       NUMERIC,
  bb_mid         NUMERIC,
  bb_lower       NUMERIC,
  week_52_high   NUMERIC,
  week_52_low    NUMERIC,
  UNIQUE (stock_id, date)
);

ALTER TABLE daily_indicators ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON daily_indicators
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_daily_indicators_stock_date ON daily_indicators (stock_id, date DESC);

-- ============================================================
-- 8. reports — ingestion state machine: download_failed → stored → confirmed (ADR 0022)
--    cse_report_id is the dedup key (ADR 0021)
--    period is null until fy_end_month is known on the parent company (ADR 0023)
-- ============================================================
CREATE TABLE reports (
  id                   UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id             UUID        NOT NULL REFERENCES stocks(id),
  company_id           UUID        NOT NULL REFERENCES companies(id),
  cse_report_id        INTEGER     NOT NULL UNIQUE,
  report_type          TEXT        NOT NULL CHECK (report_type IN ('annual', 'quarterly')),
  period               TEXT,                         -- e.g. "FY2024/25", "Q3 FY2024/25"; null if fy_end_month not yet set
  period_end_date      DATE        NOT NULL,          -- derived from manualDate (Unix ms → date)
  uploaded_date        TIMESTAMPTZ NOT NULL,          -- from uploadedDate field on CSE financials response
  cse_cdn_path         TEXT        NOT NULL,          -- original path from CSE (may lack cmt/ prefix on older reports)
  pdf_path             TEXT,                          -- Supabase Storage path; null when ingestion_status = 'download_failed'
  ingestion_status     TEXT        NOT NULL DEFAULT 'download_failed'
                         CHECK (ingestion_status IN ('download_failed', 'stored', 'confirmed')),
  is_latest            BOOLEAN     NOT NULL DEFAULT true,  -- false when superseded by a re-filing
  raw_extraction       JSONB,                         -- raw Gemini output; kept for prompt improvement auditing
  confirmed_extraction JSONB,                         -- user-confirmed extraction; source of truth for indicators
  created_at           TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE reports ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON reports
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_reports_company_id        ON reports (company_id);
CREATE INDEX idx_reports_stock_id          ON reports (stock_id);
-- Partial index for retry loop — only non-confirmed rows are retried
CREATE INDEX idx_reports_pending           ON reports (ingestion_status) WHERE ingestion_status != 'confirmed';
-- Partial index for latest-report lookups used in recommendation prompt build
CREATE INDEX idx_reports_latest_by_stock   ON reports (stock_id, period_end_date DESC) WHERE is_latest = true;

-- ============================================================
-- 9. company_fundamentals — upsert with conflict detection (ADR 0005)
--    One row per company per period; second share class triggers has_conflict flag
-- ============================================================
CREATE TABLE company_fundamentals (
  id                 UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  company_id         UUID        NOT NULL REFERENCES companies(id),
  report_id          UUID        NOT NULL REFERENCES reports(id),
  period             TEXT        NOT NULL,
  report_type        TEXT        NOT NULL CHECK (report_type IN ('annual', 'quarterly')),
  revenue            NUMERIC,
  revenue_prior      NUMERIC,
  revenue_growth_pct NUMERIC,
  net_profit         NUMERIC,
  net_profit_prior   NUMERIC,
  roe                NUMERIC,
  debt_equity        NUMERIC,
  net_assets         NUMERIC,
  has_conflict       BOOLEAN     NOT NULL DEFAULT false,
  conflict_detail    JSONB,                             -- differing values from second share class report
  created_at         TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (company_id, period)
);

ALTER TABLE company_fundamentals ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON company_fundamentals
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_company_fundamentals_company_period ON company_fundamentals (company_id, period DESC);

-- ============================================================
-- 10. stock_fundamentals — EPS and DPS are share-class-specific
-- ============================================================
CREATE TABLE stock_fundamentals (
  id             UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id       UUID        NOT NULL REFERENCES stocks(id),
  report_id      UUID        NOT NULL REFERENCES reports(id),
  period         TEXT        NOT NULL,
  report_type    TEXT        NOT NULL CHECK (report_type IN ('annual', 'quarterly')),
  eps            NUMERIC,
  eps_prior      NUMERIC,
  eps_growth_pct NUMERIC,
  dps            NUMERIC,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE (stock_id, period)
);

ALTER TABLE stock_fundamentals ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON stock_fundamentals
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_stock_fundamentals_stock_period ON stock_fundamentals (stock_id, period DESC);

-- ============================================================
-- 11. recommendations — full history retained; not overwritten nightly (ADR 0003)
-- ============================================================
CREATE TABLE recommendations (
  id           UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
  stock_id     UUID        NOT NULL REFERENCES stocks(id),
  signal       TEXT        NOT NULL CHECK (signal IN ('BUY', 'ACCUMULATE', 'HOLD', 'REDUCE', 'SELL')),
  confidence   TEXT        NOT NULL CHECK (confidence IN ('HIGH', 'MEDIUM', 'LOW')),
  reasoning    TEXT        NOT NULL,
  risks        JSONB,
  generated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

ALTER TABLE recommendations ENABLE ROW LEVEL SECURITY;
CREATE POLICY "authenticated access" ON recommendations
  FOR ALL TO authenticated USING (true) WITH CHECK (true);

CREATE INDEX idx_recommendations_stock_generated ON recommendations (stock_id, generated_at DESC);
