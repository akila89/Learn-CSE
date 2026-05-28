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

### Secrets Management
- **GitHub Actions Secrets:** `SUPABASE_URL`, `SUPABASE_SERVICE_KEY`, `GEMINI_API_KEY`
- **Vercel Environment Variables:** `SUPABASE_URL`, `SUPABASE_ANON_KEY`

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

**Discovery (verified):** Call the `financials` endpoint per stock — returns structured list of all report PDF paths:
```
POST https://www.cse.lk/api/financials
Content-Type: application/x-www-form-urlencoded
Body: symbol=JKH.N0000
```

Example response (JKH annual reports going back to 2018):
```json
{
  "infoAnnualData": [
    { "path": "cmt/upload_report_file/508_1779789508055.pdf", "fileText": "Annual Report 2025/26" },
    { "path": "cmt/upload_report_file/508_1748344127576.pdf", "fileText": "Annual Report 2024/25" }
  ]
}
```

**Report ingestion flow:**
1. Scraper detects new report on the reports page
2. Downloads PDF → stores in Supabase Storage
3. Sends PDF text to Gemini 1.5 Flash for structured extraction
4. Extracted financials presented in review UI
5. User corrects any errors + adds annotation notes (e.g. *"EPS spike — sold logistics division, one-off event"*)
6. Confirmed data saved to Supabase PostgreSQL

**Fallback:** If scraper misses a report, UI shows a warning flag on the stock. User can manually upload PDF — same Gemini extraction flow applies.

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
| **Upload Report** | Manual PDF upload → Gemini extraction → review/correct form → annotation field → save |
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
