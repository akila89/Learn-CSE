# UI Screen Specs — CSE Stock Analyser

Static HTML mockups live in `docs/mockups/` — open any file directly in a browser.
These specs are kept as reference. The mockups are the source of truth for visual design.

---

## Global style notes (include in every prompt)

> Professional financial web app with light and dark theme support. Use Inter or a similar sans-serif font. Numbers in tabular figures (font-variant-numeric: tabular-nums). Signal badges: BUY = solid green, ACCUMULATE = teal/emerald, HOLD = amber, REDUCE = orange, SELL = red. Metric cards use subtle borders, no heavy shadows. Tooltips are popovers triggered by a small question-mark icon (▸?) next to each metric label.
>
> Layout: a narrow dark sidebar on the left for navigation, plus a full-width top bar above the content area. The top bar contains: app name "CSE Analyser" on the left, and on the right a sun/moon theme toggle icon button followed by the user avatar (initials "A" in a circle) with a dropdown chevron. The theme toggle and user avatar are grouped together on the right of the top bar — this is the single location for the theme toggle across all screens.
>
> Sidebar has 2 nav items only: Stocks and Scraper Control. Upload Report has been removed — PDF upload is embedded in the Stock Detail → Reports tab. Mobile bottom nav also has 2 items: Stocks and Scraper Control.
>
> Implement full light and dark mode using Tailwind CSS dark: classes.
>
> Light theme: sidebar dark navy (#0f172a) with white text, top bar white with a subtle bottom border, content area light grey (#f8fafc), cards white with subtle grey borders, body text #0f172a.
>
> Dark theme: sidebar slightly lighter navy (#1e293b), top bar dark (#1e293b) with a subtle bottom border, content area dark (#0f172a), cards dark (#1e293b) with subtle darker borders (#334155), body text white/light (#f1f5f9), muted text (#94a3b8). All signal badge colours remain the same in both themes.

---

## Screen 1 — Login

```
Design a login screen for a personal stock analysis web app called "CSE Analyser".

Layout: centred card on a dark navy background. The card is white, rounded corners, medium width (400px), with generous padding.

Content inside the card (top to bottom):
- App name "CSE Analyser" in large bold text
- Subtitle "Colombo Stock Exchange — Personal Analysis Tool" in small muted text
- A spacer
- Label "Email" above a full-width text input
- Label "Password" above a full-width password input with a show/hide toggle icon on the right
- A full-width primary button "Sign In" in dark navy
- No sign-up link, no forgot password link, no social login — single user app

Style: clean, minimal, professional. No illustrations or decorative elements. The background is always dark navy (#0f172a) regardless of theme — the login screen does not change with the theme toggle. Include a small sun/moon theme toggle icon in the top-right corner of the page (not inside the card).
```

---

## Screen 2 — Stocks (Watchlist + Stock Manager)

```
Design a stock analysis app screen with a persistent dark sidebar on the left and a main content area on the right.

Top bar (full width, above content area):
- Left: app name "CSE Analyser" in bold
- Right: sun/moon theme toggle icon button, then user avatar circle with initials "A" and a dropdown chevron — grouped together

Sidebar (dark navy, narrow ~200px, below the top bar):
- Navigation items with icons: Stocks (active, highlighted), Scraper Control
- No user controls in the sidebar — those live in the top bar

Main content area has two horizontal tabs at the top: "Watchlist" (active) and "Stock Manager".

--- WATCHLIST TAB ---

Below the tabs:
- A search input full-width with a magnifying glass icon, placeholder "Find a stock..."
- Below the search: a responsive card grid (2 columns on desktop)

Each stock card (white, subtle border, rounded, clickable — navigates to Stock Detail):
- Top row: stock symbol in bold monospace (e.g. JKH.N0000) and company name truncated
- Price row: large price "LKR 245.75" and change "+1.20 (+1.2%)" in green (negative in red)
- Signal badge: a coloured pill badge e.g. "ACCUMULATE" in teal
- Three metric rows, each with label, value, and a small grey question-mark icon:
  - P/E    12.4  ?
  - RSI    34    ?
  - ROE    14.2% ?

Show four cards with example data:
1. JKH.N0000 / John Keells Holdings — LKR 245.75, +1.2%, ACCUMULATE, P/E 12.4, RSI 34, ROE 14.2%
2. LOLC.N0000 / LOLC Holdings — LKR 412.00, -0.8%, HOLD, P/E 8.1, RSI 52, ROE 18.7%
3. COMB.N0000 / Commercial Bank — LKR 98.50, +0.5%, BUY, P/E 7.2, RSI 29, ROE 16.1%
4. DIST.N0000 / Distilleries Company — show a "⚠ Stale — Prices as of May 27" warning banner inside the card instead of a price, no signal badge

--- STOCK MANAGER TAB (show as inactive tab, no need to design its content) ---
```

---

## Screen 3 — Stock Manager tab

```
Design the Stock Manager tab of the Stocks screen. Use the same sidebar and tab bar as the Watchlist screen (sidebar: dark navy with Stocks active, sun/moon theme toggle + user avatar "A" in the top bar (top-right); tabs: Watchlist | Stock Manager, with Stock Manager active). Apply full light/dark theme support throughout.

Content (single scrolling page, no internal tabs):

Section 1 — Search and add:
- Full-width search input, placeholder "Search all CSE stocks by name or symbol..."
- Below the input: a results list (white card, subtle border) showing 3 example results:
  - Lion Brewery Ceylon PLC   LION.N0000   [+ Add] button (outlined, small)
  - LB Finance PLC            LBF.N0000    [+ Add] button
  - Lanka Tiles PLC           TILE.N0000   [+ Add] button
- One result already on the watchlist shown greyed out with a "✓ On watchlist" label instead of the Add button

Section 2 — Active stocks heading with count (e.g. "Active (4)"):
- A list (white card, subtle border) with rows:
  - green dot  JKH.N0000   John Keells Holdings PLC     [✕ Remove] button (text, red, small)
  - green dot  LOLC.N0000  LOLC Holdings PLC            [✕ Remove]
  - green dot  COMB.N0000  Commercial Bank PLC          [✕ Remove]
  - green dot  DIST.N0000  Distilleries Company PLC     [✕ Remove]

Section 3 — Inactive stocks heading with count (e.g. "Inactive (2)"):
- A list (muted/grey background rows) with:
  - grey dot   DIPD.N0000  Dipped Products PLC          [Restore] button (outlined, small)
  - grey dot   BUKI.N0000  Bukit Darah PLC              [Restore]
```

---

## Screen 4 — Stock Detail

```
Design a stock detail screen for a financial analysis app. Same dark sidebar (Stocks nav item active, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). No tab bar at the top — this is a separate screen reached by clicking a stock card. Apply full light/dark theme support throughout.

Header (below the sidebar, full width):
- Back link "← Watchlist" top left
- Company name "John Keells Holdings PLC" in large bold
- Symbol "JKH.N0000", price "LKR 245.75", change "+1.20 (+1.2%)" on one line
- On the right of the header: a large coloured signal badge "ACCUMULATE" with "Confidence: HIGH" below it in small text
- A horizontal divider

Below the header: five horizontal tabs: Overview (active), Technicals, Fundamentals, Reports, Recommendations

--- OVERVIEW TAB ---

Recommendation reasoning card (full width, subtle teal left border):
- Text: "RSI at 34 indicates the stock is oversold. P/E of 12.4 is below its 5-year average of 15.1. Revenue has grown 18% YoY with improving margins."
- Below the text, a "Risks" section in smaller muted text: "High Debt/Equity of 1.2"

Price chart section:
- Section heading "Price Chart"
- A row of period toggle buttons: 1M  3M  6M  1Y  (1Y active)
- A placeholder chart area (dark background, 400px tall) labelled "TradingView Candlestick Chart — rendered at runtime"

--- TECHNICALS TAB (show as inactive, no content needed) ---
--- FUNDAMENTALS TAB (show as inactive, no content needed) ---
--- REPORTS TAB (show as inactive, no content needed) ---
```

---

## Screen 4b — Stock Detail: Technicals tab

```
Design the Technicals tab of the stock detail screen. Same header and sidebar as before (JKH.N0000, LKR 245.75, ACCUMULATE badge, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Tabs row with Technicals active. Apply full light/dark theme support throughout.

Content: a grid of metric cards (3 columns on desktop), each card showing:
- Metric name in small muted label
- Value in large bold
- A one-line plain-English interpretation in small text below
- A small grey question-mark icon next to the label

Six cards:
1. RSI — 34 — "Below 30 indicates oversold conditions"
2. MACD — +0.82 — "Signal line crossover — bullish momentum"
3. SMA 50 — 238 — "Above SMA 200 — short-term uptrend"
4. SMA 200 — 221 — "Long-term trend is rising"
5. Bollinger Bands — Mid 242 — "Price near midband — neutral"
6. 52-Week Range — 198 – 267 — show a small range bar with current price marker, label "Near upper range"
```

---

## Screen 4c — Stock Detail: Fundamentals tab

```
Design the Fundamentals tab of the stock detail screen. Same header and sidebar as before (JKH.N0000, ACCUMULATE, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Tabs row with Fundamentals active. Apply full light/dark theme support throughout.

Content:

Source label: "Source: Annual Report 2025/26 (Audited)" in small muted text with a green "Audited" pill badge

A grid of metric cards (3 columns desktop), each with metric name + question-mark icon, large value, and one-line plain-English description:

1. P/E Ratio — 12.4 — "How much investors pay per rupee of profit"
2. EPS — LKR 4.20 — "Company profit per share"
3. ROE — 14.2% — "How efficiently shareholder money is used"
4. Revenue Growth — +18% YoY — "Revenue grew 18% vs same period last year"
5. Debt / Equity — 1.2 — "Moderate debt relative to equity"
6. Dividend Yield — 2.1% — "Annual dividend as % of current price"
7. NAV — LKR 182 — "Net asset value per share"

Below the grid:

Stock Note section:
- Heading "Stock Note"
- A textarea with placeholder "Add a personal note about this stock..." containing example text "Management has been consistently selling shares since 2024"
- A small [Save Note] button below the textarea
```

---

## Screen 4d — Stock Detail: Reports tab

```
Design the Reports tab of the stock detail screen. Same header and sidebar as before (JKH.N0000, ACCUMULATE, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Five-tab row: Overview | Technicals | Fundamentals | Reports (active) | Recommendations. Apply full light/dark theme support throughout.

Single card "Reports" with count badge (4). No upload button in the card header — the scraper detects all reports via the CSE financials endpoint. Upload only appears inline on download_failed rows.

All rows start COLLAPSED. Accordion pattern with chevron icon that rotates on expand.

Row 1 — confirmed, annual (collapsed):
- Chevron-right icon (rotates down on expand), PDF icon, "Annual · FY2025/26" bold, green "Audited" badge
- Right: muted "Extracted · May 1" timestamp
- Expanded panel: 2-column table (Field | Value right-aligned): Revenue LKR 48.2B, Net Profit LKR 4.1B, EPS 4.20, ROE 14.2%, Debt/Equity 1.2, NAV 182, DPS 3.50. Below table: "Insights" heading + amber insight row "Operating cash flow is negative despite positive net income".

Row 2 — confirmed, quarterly (collapsed):
- Same accordion pattern. "Q3 FY2025/26", amber "Unaudited" badge, "Extracted · Mar 15"
- Expanded panel: same table structure with Q3 values. Muted "No insights detected" instead of insight row.

Row 3 — download_failed (NOT expandable — upload action always visible in collapsed row):
- 4px orange left border. No chevron.
- Left: orange warning icon, "Q2 FY2025/26" muted text, orange "Download failed" badge
- Right (in collapsed row): "Choose PDF" button (file input trigger). After selection: filename appears + teal "Upload & Extract" button appears. No expansion needed — full action visible inline.

Row 4 — superseded (collapsed, full row opacity-60 / dimmed):
- Muted chevron, PDF icon, "Annual · FY2024/25", grey "Superseded" badge, green "Audited" badge
- Right: muted "Extracted · [date]" timestamp — same pattern as confirmed rows, no "View" link (chevron communicates expandability)
- Expanded panel: older data table (no insights). Read-only — no actions.

JavaScript: toggleRow(id) toggles hidden panel + rotates chevron. File input handlers show filename and reveal Upload & Extract button for download_failed row.
```

---

## Screen 4e — Stock Detail: Recommendations tab

```
Design the Recommendations tab of the stock detail screen. Same header and sidebar as before (JKH.N0000, ACCUMULATE, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Five-tab row: Overview | Technicals | Fundamentals | Reports | Recommendations (active). Apply full light/dark theme support throughout.

Two sections:

Section 1 — Latest reasoning card (full width, subtle teal left border):
- Top-right corner: "ACCUMULATE · HIGH · May 29, 2026" in small muted text
- Quote: "RSI at 34 indicates the stock is oversold. P/E of 12.4 is below its 5-year average of 15.1. Revenue has grown 18% YoY with improving margins."
- Below: "Risks:" label + "High Debt/Equity of 1.2" in small muted text

Section 2 — "Recommendation History" card:
A list of rows, each showing date (left, muted) + signal badge + confidence (right):
- May 29, 2026   ACCUMULATE (teal)   HIGH
- May 28, 2026   ACCUMULATE (teal)   HIGH
- May 27, 2026   HOLD (amber)        MEDIUM
- May 26, 2026   HOLD (amber)        MEDIUM
- May 22, 2026   REDUCE (orange)     LOW
```

---

## Screen 5 — Scraper Control

```
Design a scraper control screen for a financial analysis app. Same dark sidebar (Scraper Control nav item active, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Apply full light/dark theme support throughout.

Page heading: "Scraper Control"

Status card (full width, subtle border, rounded):
- Left side: status indicator — a green dot + "Idle" label, and below it "Last run: May 29 2026, 3:01 AM — ✓ 28/28 stocks"
- Right side: a primary button [▶ Run Full Scrape] in dark navy

Stale Stocks section:
- Section heading "Stale Stocks" with a count badge (2)
- Two warning rows (amber left border or amber icon):
  - ⚠ DIST.N0000  Distilleries Company   Prices as of May 27
  - ⚠ DIPD.N0000  Dipped Products        Prices as of May 26
- Below the rows: [↺ Retry Stale Stocks] outlined button

Recent Runs section:
- Section heading "Recent Runs"
- A table with columns: Date | Status | Stocks Done | Stocks Failed
- Rows:
  - May 29 2026  3:01 AM  |  ✓ Success  (green badge)  |  28  |  0
  - May 28 2026  3:00 AM  |  ⚠ Partial  (amber badge)  |  26  |  2   ← the 2 is a red pill badge, clickable
  - May 27 2026  3:02 AM  |  — Holiday  (grey badge)   |  —   |  —
  - May 26 2026  3:01 AM  |  ✓ Success  (green badge)  |  28  |  0

Style note: "In progress" status would show a blue spinning indicator + "Running..." with a [View on GitHub →] link — not shown here but leave visual room for it.
```

---

## UX States — loading, error, and success feedback

Mockups show the happy-path end state only. Implement these states at build time.

### Stock Detail Reports tab — accordion row states

| ingestion_status | Collapsed row shows | Expandable? |
|---|---|---|
| `confirmed` | Chevron · PDF icon · period label · audit badge · "Extracted · [date]" | Yes — extraction table + insights |
| `stored` (extraction pending) | Chevron · PDF icon · period label · audit badge · amber "Extraction pending" | Yes — table shown if partial; insight row shows "Extraction in progress" |
| `download_failed` | Warning icon · muted period label · orange "Download failed" badge · "Choose PDF" file input · filename + "Upload & Extract" button after selection | No chevron — action is always visible in collapsed row |
| superseded (`is_latest = false`) | Dimmed chevron · PDF icon · "Superseded" badge · audit badge · muted "Extracted · [date]" timestamp | Yes — read-only older data table |

### Reports tab — PDF upload (download_failed rows only)

| Phase | What the user sees |
|---|---|
| No file chosen | "Choose PDF" file input button visible in the collapsed download_failed row |
| File chosen | Filename appears inline. Teal "Upload & Extract" button appears. |
| Uploading + extracting | Button shows spinner + "Extracting…" label, disabled. Takes 10–60 seconds. |
| Success | Row transitions: `download_failed` → `confirmed`. Expanded panel shows extraction table + insights. |
| Gemini error | Amber inline error: "Extraction failed — try re-selecting the file." Retry available. |
| Network / storage error | Red inline error. Retry available. |

### Scraper Control — Run Full Scrape / Retry (minutes, runs on GitHub Actions)

| Phase | What the user sees |
|---|---|
| Triggering | "Run Full Scrape" button shows spinner, disabled for 3 seconds while the webhook fires |
| Triggered | Status card dot turns blue, label "Running — triggered on GitHub Actions". [View on GitHub →] link. Button disabled. |
| Completion | Detected on next page load / manual refresh (no real-time push in v1). Status reverts to Idle or Partial. |
| Trigger error | Red banner: "Could not trigger scraper — check GitHub Actions." |

### General — data loading on page entry

| Screen | Loading state |
|---|---|
| Watchlist | Skeleton cards (grey animated placeholder) while fetching |
| Stock Detail | Skeleton for header price + indicator cards |
| Scraper Control | Skeleton rows in the Recent Runs table |
| All | If Supabase returns an error: full-page error state with "Could not load data — refresh to try again" |

---

## Responsive Strategy

Breakpoints used (Tailwind defaults, no custom config needed):

| Prefix | Min-width | Usage |
|---|---|---|
| *(none)* | 0px | Mobile-first base styles |
| `sm:` | 640px | 2-column grids, side-by-side headers, wider selectors |
| `lg:` | 1024px | Sidebar visible, 3-column grids, full desktop layout |

`md:` (768px) is not used — the jump from mobile to desktop is `lg:`.

### Layout

- **Sidebar** (`w-48 bg-slate-900`): `hidden lg:flex` — hidden on mobile/tablet, shown on desktop.
- **Bottom nav**: `lg:hidden` fixed bar pinned to viewport bottom with 2 items (Stocks, Scraper Control). Active item in `teal-400`, inactive in `slate-500`. Each screen sets its own active item. Upload Report removed — PDF upload is embedded in the Reports tab.
- **Main content** bottom padding: `pb-24 lg:pb-0` on all authenticated screens so the last card scrolls clear of the bottom nav.
- **Content horizontal padding**: `px-4 sm:px-6` — tighter on mobile, standard on tablet+.
- **Login**: no sidebar, no bottom nav. Centered card with `max-w-md mx-auto` — responsive by default.

### Grids

| Content | Mobile | sm (640px+) | lg (1024px+) |
|---|---|---|---|
| Watchlist stock cards | 1 col | 2 col | 2 col |
| Technicals indicator cards | 1 col | 2 col | 3 col |
| Fundamentals metric cards | 1 col | 2 col | 3 col |

### Stock Detail header

Below `sm`: company name, price row, and signal badge stack vertically (`flex-col`).  
At `sm+`: side-by-side (`sm:flex-row sm:justify-between`), price row wraps with `flex-wrap gap-2`.

### Tab bars and tables

- Tab bars: `overflow-x-auto` — scroll horizontally on narrow screens, no wrapping.
- Tables (Scraper Control recent runs): wrapped in `<div class="overflow-x-auto">` with `min-w-[480px]` on the `<table>` so columns don't collapse below readable width.
