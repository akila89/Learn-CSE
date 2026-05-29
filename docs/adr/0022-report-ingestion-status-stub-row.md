# Report ingestion status with stub row on download failure

When the scraper detects a new `cse_report_id` but fails to download the PDF from the CDN, it immediately inserts a stub `reports` row with `ingestion_status = 'download_failed'` and `pdf_path = null`. On each subsequent nightly run the scraper retries all `download_failed` rows.

**Why not retry-only with no DB row:** Without a row, the UI cannot surface "this report is pending" to the user. After a manual upload the scraper would still see the `cse_report_id` absent and attempt download again, risking a duplicate row and redundant Gemini extraction.

**Why not hard-fail immediately:** A CDN failure is often transient. Automatic retry over a few days recovers most cases without user intervention. Only persistent failures require manual upload (ADR 0012).

**`ingestion_status` state machine:**

| Status | Meaning | Next action |
|---|---|---|
| `download_failed` | PDF not yet fetched; `pdf_path` is null | Scraper retries each run; user can manually upload |
| `stored` | PDF in Supabase Storage; extraction may or may not have run (check `raw_extraction`) | Scraper (or Edge Function) sends to Gemini |
| `confirmed` | Gemini extraction complete; fundamentals written to `company_fundamentals` + `stock_fundamentals` | Nothing — done |

Manual upload (ADR 0012) targets the existing stub row when one exists for the stock and period, filling in `pdf_path` and triggering extraction via the `extract-report` Edge Function. This keeps `cse_report_id` on the row so the scraper does not re-attempt download on the next run.
