# Company/Stock entity split

The CSE API treats every tradeable instrument as a flat "Security," but the domain needs a two-level model. A **Company** owns financial reports and fundamentals that are independent of share price; a **Stock** is a specific share class (`.N0000` voting or `.X0000` non-voting) that owns its own price series and price-derived indicators. This split is necessary because a single company (e.g. COMB) can have two share classes with different prices and therefore different P/E ratios and dividend yields, while sharing the same annual report, revenue, ROE, and EPS figures. Flattening everything into a single "stock" table would either duplicate company-level data across share classes or make it impossible to correctly attribute figures like EPS (which can differ between `.N` and `.X` due to different share counts).

## Considered Options

- **Flat stocks table** — one row per symbol, all data duplicated across share classes. Rejected: annotations and report data would diverge silently.
- **Company/Stock split (chosen)** — company-level figures stored once on Company; stock-level figures stored per Stock. Deduplication happens at ingest time; conflicts across share classes are flagged for user review.