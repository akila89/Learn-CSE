# Company fundamentals conflict detection across share classes

When both `.N` and `.X` share classes of the same company are watchlisted, the scraper ingests quarterly reports for each symbol separately. Company-level figures (revenue, net profit, ROE, etc.) should be identical across both reports, but PDF formatting differences or filing errors can produce disagreements. Rather than failing on insert or silently overwriting, the scraper upserts into `company_fundamentals` and compares incoming figures against what is already stored. If any figure differs, it sets `has_conflict = true` and writes the differing values into `conflict_detail JSONB` for user review. The row is not blocked — the first-ingested values remain and the conflict is flagged.

Silent first-wins was rejected because a wrong figure would flow into Recommendations without any visibility. Hard-fail on conflict was rejected because it would block the entire scrape run over a likely minor formatting discrepancy.
