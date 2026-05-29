# CSE Report ID as report dedup key

The scraper detects new reports by comparing `id` values from the CSE `financials` endpoint against `reports.cse_report_id` in the database. Each entry in `infoAnnualData` and `infoQuarterlyData` carries a stable integer `id` assigned by CSE at upload time — this is the dedup key, not the `path` field.

The `path` was the initial candidate but was rejected: CDN paths for older reports lack a consistent prefix (`cmt/upload_report_file/` vs `upload_report_file/`), making normalisation fragile. CSE could also re-upload a corrected file under the same path. The `id` uniquely identifies each distinct filing event and is stable once assigned.

A new report is any `id` in the `financials` response not yet present in `reports.cse_report_id`. Re-filings appear as a new `id` with the same derived `period` and `report_type` as an existing row — the scraper detects this, sets the old row `is_latest = false`, and ingests the new one fresh.
