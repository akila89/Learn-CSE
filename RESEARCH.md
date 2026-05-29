# CSE Stock Analyser — Research & Design Document

**Date:** 2026-05-28  
**Author:** Akila Iroshan  
**Purpose:** Personal stock analysis tool for the Colombo Stock Exchange (cse.lk) with the end goal of generating informed buy/sell recommendations.

---

## 1. Project Goal

Build a personal web application that:
- Tracks a self-selected watchlist of CSE-listed stocks
- Performs fundamental and technical analysis automatically
- Generates plain-English buy/sell/hold recommendations using AI
- Updates data autonomously (price scraping + report ingestion)
- Runs entirely on free-tier infrastructure with no credit card required

**Not** a general-purpose stock screener — personal decision-support tool only. Single user initially.

---

## 2. Tech Stack (All Free, No Credit Card)

| Layer | Tool | Why |
|---|---|---|
| Scraper + cron | GitHub Actions (.NET) | 2000 min/month free on public repo, cron + manual trigger |
| Database | Supabase PostgreSQL | 500MB free, no CC, auto-REST API included |
| File storage | Supabase Storage | 1GB free, for PDF report storage |
| Auth | Supabase Auth | Built into Supabase, single user login |
| Frontend hosting | Vercel | Free, always-on, Angular static builds |
| PDF extraction | Gemini 1.5 Flash API | Free tier (1M tokens/day), no CC |
| UI design | v0.dev | AI UI generation from prompts, free tier |
| Charts (price) | TradingView Lightweight Charts | Open source (Apache 2.0), professional candlestick |
| Charts (fundamentals) | ApexCharts (ng-apexcharts) | Free, Angular-native |

**Frontend framework:** Angular + TypeScript (developer has existing experience)

### Secrets Inventory

Every secret, where it is stored, and what uses it. On key rotation, update **all** rows for that secret.

| Secret | Store | Used by |
|---|---|---|
| `GEMINI_API_KEY` | GitHub Actions Secrets (scraper repo) | Scraper — report extraction + nightly recommendations |
| `GEMINI_API_KEY` | Supabase Edge Function vault | `extract-report` Edge Function — manual PDF upload |
| `SUPABASE_SERVICE_KEY` | GitHub Actions Secrets (scraper repo) | Scraper — direct PostgreSQL writes via Npgsql |
| `SUPABASE_URL` | GitHub Actions Secrets (scraper repo) | Scraper — PostgreSQL connection string |
| `SUPABASE_URL` | Vercel environment variables | Angular build — Supabase JS client |
| `SUPABASE_ANON_KEY` | Vercel environment variables | Angular build — Supabase JS client |
| `GITHUB_PAT` | Supabase Edge Function vault | `trigger-scraper` Edge Function — proxies workflow_dispatch |
| `SUPABASE_ACCESS_TOKEN` | GitHub Actions Secrets (frontend repo) | Edge Function deployment workflow (ADR 0015) |
| `SUPABASE_PROJECT_REF` | GitHub Actions Secrets (frontend repo) | Edge Function deployment workflow (ADR 0015) |
| `SUPABASE_DB_PASSWORD` | Local developer machine only | `supabase db push` for schema migrations (ADR 0017) |

**Note:** `GEMINI_API_KEY` exists in two separate stores. If this key is rotated, it must be updated in both GitHub Actions Secrets and the Supabase Edge Function vault — updating only one will silently break either nightly recommendations or manual PDF extraction.

---

## 3. Data Sources

### 3.1 Price Data — CSE API (Verified Working)

CSE exposes an unofficial API. No official documentation — endpoints may change without notice. **Verified working 2026-05-28 via Playwright reverse engineering.**

**Critical findings (all verified 2026-05-28):**
- Endpoints return 400 with `application/json` — must use `application/x-www-form-urlencoded`
- **No authentication required** — no login, no session cookie
- **No Referer header required** — plain HTTP client works
- **No x-api-key required** — key exists in JS bundle (`btoa("Cse123Api")` = `Q3NlMTIzQXBp`) but is only needed for user account endpoints (login, OTP, subscriptions)
- Simplest working scraper: plain `HttpClient` POST with form-encoded body, no special headers

**Base URL:** `https://www.cse.lk/api/`

**Verified working endpoints:**

| Endpoint | Method | Content-Type | Params | Returns |
|---|---|---|---|---|
| `allSecurityCode` | GET | — | — | All stocks: numeric id, symbol, name |
| `companyChartDataByStock` | POST | form-urlencoded | `stockId={numericId}&period=5` | OHLCV data |
| `companyInfoSummery` | POST | multipart/form-data | `symbol=JKH.N0000` | Company metadata, security ID |
| `financials` | POST | form-urlencoded | `symbol=JKH.N0000` | Annual report PDF path list |
| `todaySharePrice` | POST | any | (empty) | All stocks current price + change |
| `tradeSummary` | POST | any | (empty) | All stocks summary |
| `marketStatus` | POST | any | (empty) | `{"status":"Market Closed"}` |

**Stock IDs:** Two separate IDs per stock. Example for JKH:
- `allSecurityCode` id: `297` — used for `companyChartDataByStock`
- `companyInfoSummery` securityId: `508` — used for news endpoint

**Historical price data call (verified):**
```
POST https://www.cse.lk/api/companyChartDataByStock
Content-Type: application/x-www-form-urlencoded
Body: stockId=297&period=5
```

**Period values (verified):**

| Period | Returns | Records | Use case |
|---|---|---|---|
| `1` | Intraday minute data (today) | ~670 | Real-time |
| `3` | 1 month daily OHLCV | ~20 | Short-term |
| `5` | **1 year daily OHLCV** | **~241** | **Technical analysis** |
| `2` | ~1 week | 4 | — |

**Response fields:** `h` (high), `l` (low), `o` (open), `p` (price/close), `q` (quantity/volume), `t` (timestamp ms), `c` (change), `pc` (% change)

**Stock symbol format:** `TICKER.N0000` (e.g., `JKH.N0000`, `LOLC.N0000`)

**Limitation:** `period=5` only returns ~1 year. No multi-year history available via this API. For historical backfill beyond 1 year, call the API once and cache — data won't change.

**Risk:** Unofficial API — scraper must have graceful failure handling. Manual trigger fallback covers outages.

### 3.2 Annual & Quarterly Reports — CDN + Page Monitoring

Reports are published as PDFs on CSE's CDN:
```
https://cdn.cse.lk/cmt/upload_report_file/[ID]_[TIMESTAMP].pdf
```

**Discovery (verified 2026-05-29):** Call the `financials` endpoint per stock — returns all report types in a single response:
```
POST https://www.cse.lk/api/financials
Content-Type: application/x-www-form-urlencoded
Body: symbol=JKH.N0000
```

**Full response structure (verified against JKH.N0000, COMB.N0000, LOLC.N0000):**

| Top-level key | Contents | Count (JKH) | v1 use |
|---|---|---|---|
| `infoAnnualData` | Annual report PDFs, full history | 15 | ✅ Ingest |
| `infoQuarterlyData` | Quarterly/interim report PDFs, full history | 58 | ✅ Ingest |
| `infoOtherData` | Press releases, prospectuses, debenture trust deeds, errata | 8 | ⏭ Skip (see below) |
| `infoWebLink` | Always empty array | 0 | ⏭ Skip |
| `reqFinancial` | Structured financial summary rows from CSE (159 items, `secId`, `elmId`, `data`) | 159 | ⏭ Skip (see below) |
| `infoCompanyBannerAd` | Company banner ad image path | — | ⏭ Skip |

**Per-report item fields (all arrays share the same shape):**
```json
{
  "id": 51321,
  "path": "cmt/upload_report_file/508_1779789508055.pdf",
  "manualDate": 1779733800000,
  "uploadedDate": 1779789508055,
  "fileText": "Annual Report as at 31st March 2026",
  "path2": null,
  "authorizedDate": 1779794879336
}
```

| Field | Meaning | Notes |
|---|---|---|
| `id` | CSE Report ID — **dedup key** | Stable integer assigned by CSE on upload |
| `path` | PDF path under `https://cdn.cse.lk/` | Older reports may lack the `cmt/` prefix |
| `manualDate` | Financial period end date (Unix ms) | Reliable — use this to derive period label |
| `uploadedDate` | Timestamp PDF was uploaded to CSE | Used to detect re-filings (newer upload = correction) |
| `fileText` | Human-readable label | **Unreliable** — format varies wildly per company and era |
| `path2` | Alternate file path, sometimes `.xlsx` | Some quarterly reports have Excel attachments alongside PDF |
| `authorizedDate` | CSE authorization timestamp | Null for many reports; meaning unclear — not used in v1 |

**`fileText` inconsistency examples (same field, same endpoint, different companies):**
- "Annual Report as at 31st March 2026"
- "ANNUAL REPORT FOR 2024/2025"
- "Commercial Bank of Ceylon PLC - Annual Report 2024"
- "Interim Financial Statements for the Quarter ended 31st December 2025"
- "INTERIM FINANCIAL STATEMENTS AS OF 06/30/2025"
- "Quarterly Financial Statements as of 31-12- 2011"

→ Never parse `fileText` to derive period — always use `manualDate`.

**`infoOtherData` — what's available (skipped in v1):**
- Press releases (e.g. "Press Release 31/03/2018")
- Prospectuses (e.g. "Prospectus - Debenture Issue 2021")
- Debenture trust deeds
- Errata to annual reports (e.g. "Errata to the Annual Report 2022/2023")

**Future use:** If a company issues an errata, the corrected numbers should arrive via a re-filing in `infoAnnualData` or `infoQuarterlyData`. The errata PDF itself (in `infoOtherData`) is a narrative correction document — could be fetched and sent to Gemini in v2 to generate an Insight flagging what was corrected.

**`reqFinancial` — what's available (skipped in v1):**
- 159 rows per company, all with `elmId: "1"` and `data: "Financial Statements Summary"`
- Appears to be structured financial summary data pre-extracted by CSE from the reports
- All three test stocks returned exactly 159 rows — likely a fixed schema of financial line items

**Future use:** If CSE's pre-extracted financial data is reliable, `reqFinancial` could supplement or cross-check Gemini extraction in v2, reducing reliance on PDF parsing for standard line items.

**Report ingestion flow (v1):**
1. Scraper calls `financials` for each Active Stock
2. Finds `id` values not yet in `reports.cse_report_id` → new reports
3. For each new report: creates stub row (`ingestion_status = 'download_failed'`), attempts PDF download from CDN
4. Download success → uploads to Supabase Storage, sends to Gemini, writes fundamentals, sets `ingestion_status = 'confirmed'`
5. Download failure → stub row remains; scraper retries on subsequent runs
6. If after several days still failing → UI surfaces "manual upload required"

**Fallback:** User can manually upload a PDF from the Reports tab in Stock Detail (ADR 0012). Manual upload targets the stub row if one exists, or creates a new row. Same Gemini extraction flow applies.

---

## 4. Analysis Indicators

All indicators are **pre-computed at scrape/ingest time** and stored in the database. The Angular frontend only reads and displays — no client-side calculation.

Every metric displayed in the UI includes a **plain-English explanation** (tooltip or expandable description) since the primary user is not a financial professional.

### 4.1 Fundamental Indicators

Sourced from annual/quarterly report data (PDF extraction):

| Indicator | Description shown in UI |
|---|---|
| **P/E Ratio** | Price-to-Earnings — how much investors pay per rupee of profit |
| **EPS** | Earnings Per Share — company profit divided by number of shares |
| **ROE** | Return on Equity — how efficiently the company uses shareholder money |
| **Revenue Growth YoY** | How much revenue grew compared to same period last year |
| **Debt/Equity** | How much debt the company carries relative to shareholder equity |
| **Dividend Yield** | Annual dividend as a percentage of current share price |
| **NAV** | Net Asset Value — the company's net worth per share |

### 4.2 Technical Indicators

Sourced from daily price data (API scraping):

| Indicator | Description shown in UI |
|---|---|
| **RSI (14-day)** | Relative Strength Index — above 70 = possibly overbought, below 30 = possibly oversold |
| **MACD** | Trend momentum indicator — signal line crossovers show potential reversals |
| **SMA 50** | 50-day Simple Moving Average — short-term trend direction |
| **SMA 200** | 200-day Simple Moving Average — long-term trend direction |
| **Bollinger Bands** | Volatility bands — price near upper band = expensive, near lower = cheap |
| **52-Week High/Low** | Price range over the past year — context for current price |

---

## 5. Buy/Sell Recommendation Engine

**This is the primary purpose of the app** — not just a dashboard.

### Flow
1. After each scrape cycle, collect all current fundamental + technical indicators for a stock
2. Include any user annotations on recent reports (e.g. one-off events)
3. Send structured data to **Gemini 1.5 Flash** with a prompt that asks for:
   - Signal: `BUY` / `ACCUMULATE` / `HOLD` / `REDUCE` / `SELL`
   - Confidence level
   - Plain-English reasoning (3–5 sentences)
   - Key risks flagged
4. Store recommendation + full reasoning in DB
5. Display prominently on stock card and stock detail page

### Example output
> **ACCUMULATE** — RSI at 28 indicates the stock is oversold. P/E of 8.2 is below its 5-year average of 11.4. Revenue has grown 18% YoY with improving margins. Note: the EPS spike in Q3 2024 was a one-off asset sale (flagged in your annotation) — underlying earnings are flat. Key risk: high Debt/Equity of 1.8.

### Future additions (not in initial build)
- Sentiment analysis from news sources
- Peer comparison within CSE sector
- Portfolio-level recommendations

---

## 6. Application Screens

| Screen | Purpose |
|---|---|
| **Login** | Supabase Auth — single user |
| **Watchlist / Dashboard** | All tracked stocks with quick summary: price, signal badge, key metrics |
| **Stock Detail** | Full analysis — candlestick chart, technical indicators panel, fundamental metrics cards, recommendation with reasoning, historical report entries with annotations |
| **Scraper Control** | Trigger price or report scrape manually, view last run status and any failure flags |
| **Stock Manager** | Add/remove stocks from watchlist (symbol search) |

**UI Design Process:** Design all screens in **v0.dev** first using plain-English prompts. Use generated designs as implementation reference for Angular development.

---

## 7. Architecture Overview

```
┌─────────────────────────────────────────────────┐
│              GitHub Actions (Public Repo)        │
│  ┌──────────────────┐  ┌───────────────────────┐ │
│  │  Price Scraper   │  │   Report Scraper      │ │
│  │  (.NET, nightly) │  │   (.NET, nightly)     │ │
│  │  cse.lk JSON API │  │   cse.lk CDN monitor  │ │
│  └────────┬─────────┘  └──────────┬────────────┘ │
│           │                       │               │
│           ▼                       ▼               │
│  ┌──────────────────────────────────────────────┐ │
│  │        Technical Indicator Calculator        │ │
│  │    (RSI, MACD, SMA, Bollinger Bands)        │ │
│  └────────────────────┬─────────────────────────┘ │
│                       │                           │
│                       ▼                           │
│  ┌────────────────────────────────────────────┐  │
│  │         Gemini 1.5 Flash API               │  │
│  │   PDF extraction + Recommendation engine   │  │
│  └────────────────────┬───────────────────────┘  │
└───────────────────────┼──────────────────────────┘
                        │
                        ▼
┌───────────────────────────────────────────────────┐
│                   Supabase                        │
│   PostgreSQL (data) + Storage (PDFs) + Auth       │
└───────────────────────┬───────────────────────────┘
                        │ Auto-REST API
                        ▼
┌───────────────────────────────────────────────────┐
│              Vercel (Angular SPA)                 │
│   TradingView Lightweight Charts + ApexCharts     │
│         Supabase Auth + Anon Key reads            │
└───────────────────────────────────────────────────┘
```

---

## 8. Key Risks & Mitigations

| Risk | Mitigation |
|---|---|
| CSE unofficial API changes | Manual trigger fallback; graceful error handling in scraper; UI flags stale data |
| CSE unofficial API rate limits are undocumented | At ~30 stocks with 4-second inter-call delay, actual rate is ~8 req/min — well below any typical limit. If throttling occurs, the scraper's existing graceful failure handling logs affected stocks as failed and the targeted retry (ADR 0008) recovers them. No architectural change needed. |
| Gemini API changes or rate limits | PDF extraction is not time-critical; retry logic in scraper |
| Supabase free tier limits (500MB DB, 1GB storage) | Estimated usage ~6MB for 30 stocks × 5 years; well within limits |
| GitHub Actions minutes exhausted | 2000 min/month free; nightly scraper ~5 min = 150 min/month used |
| Report PDF format varies | User review step before saving; manual override for all extracted values |
| Recommendation quality | Recommendations are decision-support only; user retains final judgement; annotations provide context for one-off events |

---

## 9. Next Steps

1. **Design UI screens** in v0.dev — all 6 screens, screenshot each
2. **Verify CSE API** — test `companyChartDataByStock` with different `period` values to confirm historical data depth
3. **DB schema design** — tables for stocks, daily_prices, indicators, fundamentals, reports, recommendations
4. **GitHub Actions workflow** — .NET scraper project structure, cron schedule, manual trigger
5. **Angular project scaffold** — Supabase client, routing, auth guard
6. **PDF extraction prompt engineering** — Gemini prompt to reliably extract the 7 fundamental indicators from CSE report PDFs
