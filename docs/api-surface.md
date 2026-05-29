# Angular Frontend — API Surface

## 1. Supabase Auth

Handled directly by the Supabase JS client. No custom endpoints needed.

| Operation | Method |
|---|---|
| Sign in (email + password) | `supabase.auth.signInWithPassword()` |
| Sign out | `supabase.auth.signOut()` |
| Session state | `supabase.auth.onAuthStateChange()` |

---

## 2. Supabase PostgREST (direct table access)

All requests use the anon key + active session. RLS policies enforce access.

### Dashboard / Watchlist

| Operation | Table(s) | Notes |
|---|---|---|
| Get active watchlist with stock details | `watchlist_entries` + `stocks` + `companies` | Filter `is_active = true` for current user |
| Get latest recommendation per stock | `recommendations` | Order by `generated_at DESC`, limit 1 per stock |
| Get latest price per stock | `daily_prices` | Order by `date DESC`, limit 1 per stock |

### Stock Detail

| Operation | Table(s) | Notes |
|---|---|---|
| Get price history | `daily_prices` | All rows for stock, ordered by date |
| Get indicator history | `daily_indicators` | All rows for stock, ordered by date |
| Get latest company fundamentals | `company_fundamentals` | Filter `is_latest = true` (via report join), most recent period |
| Get latest stock fundamentals | `stock_fundamentals` | Same |
| Get company profile + insights | `companies` | Single row by company_id |
| Get recommendations history | `recommendations` | Ordered by `generated_at DESC` |
| Get reports list | `reports` | Filter `is_latest = true`, ordered by `ingested_at DESC` |
| Get user annotations for a report | `field_annotations` | Filter by `report_id` + current `user_id` |
| Add / edit user annotation | `field_annotations` | Upsert on `(report_id, field_name, user_id)` |
| Update stock note | `watchlist_entries` | Patch `stock_note` for current user + stock |
| Upload PDF to storage | Supabase Storage | `reports` bucket — triggered from `download_failed` rows in Reports tab |
| Save report row + extraction | `reports` | Insert with `raw_extraction` + `extraction` populated |
| Save confirmed fundamentals | `company_fundamentals` + `stock_fundamentals` | After extraction completes |
| Update extraction (user correction) | `reports` | Patch `extraction` + set `corrected_at` |

### Stock Manager

| Operation | Table(s) | Notes |
|---|---|---|
| Search all CSE stocks | `cse_all_stocks` | Text search on `name` or `symbol` |
| Toggle watchlist active/inactive | `watchlist_entries` | Patch `is_active`, set `removed_at` on deactivation |
| Insert new company | `companies` | Only if company does not already exist |
| Insert new stock | `stocks` | With both CSE IDs resolved via `resolve-stock` edge function |
| Insert watchlist entry | `watchlist_entries` | After stock is created |

### Scraper Control

| Operation | Table(s) | Notes |
|---|---|---|
| Get last N scraper runs | `scraper_runs` | Ordered by `run_at DESC` |
| Get stale stocks | `stocks` | Filter `last_price_scraped_at` below threshold |

---

## 3. Supabase Edge Functions

### `trigger-scraper`

Proxies GitHub Actions API. Holds the GitHub PAT as a server-side secret.

**Trigger full run:**
```
POST /functions/v1/trigger-scraper
Body: {}
```

**Trigger targeted retry:**
```
POST /functions/v1/trigger-scraper
Body: { "stock_symbols": ["JKH.N0000", "LOLC.N0000"] }
```

**Get current run status:**
```
GET /functions/v1/trigger-scraper
Returns: { status: "in_progress" | "queued" | "completed" | "idle", run_id, started_at }
```

---

### `extract-report`

Sends a stored PDF to Gemini and returns the structured extraction. Holds the Gemini API key as a server-side secret.

```
POST /functions/v1/extract-report
Body: { "pdf_path": "reports/508_1779789508055.pdf" }
Returns: { extraction: <ExtractionSchema JSON> }
```

---

### `resolve-stock`

Resolves a CSE symbol to its `cse_security_id` and company name by calling the CSE `companyInfoSummery` API server-side (avoids browser CORS). Called once when a user adds a new stock from the Stock Manager.

```
POST /functions/v1/resolve-stock
Body: { "symbol": "JKH.N0000" }
Returns: { cse_security_id: 508, company_name: "John Keells Holdings PLC" }
```

---

## 4. Summary — Edge Functions

| Function | Secrets required | Called from |
|---|---|---|
| `trigger-scraper` | `GITHUB_PAT` | Scraper Control |
| `extract-report` | `GEMINI_API_KEY` | Stock Detail (Reports tab) |
| `resolve-stock` | — (no secrets; CSE API is public) | Stock Manager |
