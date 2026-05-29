# Period label derived from manualDate and Financial Year End Month

Report periods are stored as human-readable canonical labels derived by the scraper from two inputs:

- **`manualDate`** — the financial period end date as a Unix timestamp, present on every entry in the CSE `financials` response. Consistent and machine-readable across all companies and historical eras.
- **`companies.fy_end_month`** — the month (1–12) in which the company closes its financial year. Auto-set from the month of the first Annual Report's `manualDate`. Most CSE companies use March (3); banks and some others use December (12). Null until first annual report is ingested.

The `fileText` field was rejected as the period source. Verified against JKH, COMB, and LOLC: format varies drastically across companies, eras, and languages — "Annual Report 2024/25", "ANNUAL REPORT FOR 2024/2025", "Annual Report as at 31st March 2026", "INTERIM FINANCIAL STATEMENTS AS OF 06/30/2025". Reliable parsing would require a large pattern library and would still fail on novel formats.

**Canonical period format:**

| Report type | FY end | manualDate month | Period label |
|---|---|---|---|
| Annual | March (3) | March | `FY2024/25` |
| Quarterly | March (3) | June | `Q1 FY2024/25` |
| Quarterly | March (3) | September | `Q2 FY2024/25` |
| Quarterly | March (3) | December | `Q3 FY2024/25` |
| Quarterly | March (3) | March | `Q4 FY2024/25` |
| Annual | December (12) | December | `FY2025` |
| Quarterly | December (12) | March | `Q1 2025` |
| Quarterly | December (12) | June | `Q2 2025` |
| Quarterly | December (12) | September | `Q3 2025` |
| Quarterly | December (12) | December | `Q4 2025` |

The same period end date means different quarter labels depending on `fy_end_month` — December 31 is Q3 for a March-FY company but Q4 for a December-FY company. This is why `fy_end_month` is required rather than deriving the label from date alone.

`fy_end_month` is null for a company that has no ingested annual reports yet. In that state the scraper can ingest quarterly reports but cannot compute the quarter label — it should defer labelling until `fy_end_month` is set by the first annual ingestion. Practically this is rare: backfill ingests all historical reports, so `fy_end_month` is set on the first run for any stock with historical annual reports on CSE.
