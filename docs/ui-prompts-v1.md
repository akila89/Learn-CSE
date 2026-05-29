# UI Screen Specs — CSE Stock Analyser

Static HTML mockups live in `docs/mockups/` — open any file directly in a browser.
These specs are kept as reference. The mockups are the source of truth for visual design.

---

## Global style notes (include in every prompt)

> Professional financial web app with light and dark theme support. Use Inter or a similar sans-serif font. Numbers in tabular figures (font-variant-numeric: tabular-nums). Signal badges: BUY = solid green, ACCUMULATE = teal/emerald, HOLD = amber, REDUCE = orange, SELL = red. Metric cards use subtle borders, no heavy shadows. Tooltips are popovers triggered by a small question-mark icon (▸?) next to each metric label.
>
> Layout: a narrow dark sidebar on the left for navigation, plus a full-width top bar above the content area. The top bar contains: app name "CSE Analyser" on the left, and on the right a sun/moon theme toggle icon button followed by the user avatar (initials "A" in a circle) with a dropdown chevron. The theme toggle and user avatar are grouped together on the right of the top bar — this is the single location for the theme toggle across all screens.
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
- Navigation items with icons: Stocks (active, highlighted), Upload Report, Scraper Control
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

Below the header: four horizontal tabs: Overview (active), Technicals, Fundamentals, Reports

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
Design the Reports tab of the stock detail screen. Same header and sidebar as before (JKH.N0000, ACCUMULATE, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Tabs row with Reports active. Apply full light/dark theme support throughout.

Content split into two sections:

Section 1 — "Reports" heading:
A list of report rows (white card, subtle border), each row showing:
- Report type and period: e.g. "Annual Report 2025/26"
- Audit status badge: "Audited" (green pill) or "Unaudited" (amber pill)
- A [Review →] link on the right

Rows:
- Annual Report 2025/26    Audited      [Review →]
- Q3 Report 2025/26        Unaudited    [Review →]
- Annual Report 2024/25    Audited      [Review →]

Section 2 — "Recommendation History" heading:
A simple table or list:
- May 29 2026   ACCUMULATE   HIGH     (teal badge)
- May 28 2026   ACCUMULATE   HIGH     (teal badge)
- May 27 2026   HOLD         MEDIUM   (amber badge)
- May 26 2026   HOLD         MEDIUM   (amber badge)
- May 22 2026   REDUCE       LOW      (orange badge)
```

---

## Screen 5 — Upload Report

```
Design an upload report screen for a financial analysis app. Same dark sidebar (Upload Report nav item active, sun/moon theme toggle + user avatar "A" in the top bar (top-right)). Apply full light/dark theme support throughout.

Page heading: "Upload Report"

Step 1 — Stock selector:
- Label "Stock"
- A dropdown selector showing "JKH.N0000 — John Keells Holdings PLC" with a chevron

Step 2 — PDF upload:
- A large dashed-border drop zone (full width, ~150px tall, rounded corners, light grey background)
- Centred content: upload icon, text "Drop PDF here or click to browse"
- Below the drop zone (after file is selected): filename shown e.g. "annual_report_2026.pdf  ✓ Uploaded" in small text

Step 3 — Extraction results (shown after upload and extraction completes):
- Section heading "Extraction Results" with a subtitle "Annual Report 2025/26 — ● Extracted"
- A table with columns: Field | Extracted Value | Annotate
- Rows:
  - Revenue       | LKR 48.2B  | [+ Add note] button
  - Net Profit    | LKR 4.1B   | [+ Add note] button
  - EPS           | 4.20       | [✎ Note set] button (in teal, indicating a note exists)
    - Below the EPS row (expanded inline): a small note card "One-off gain from logistics division asset sale — not recurring" with an [Edit] and [Remove] link
  - ROE           | 14.2%      | [+ Add note]
  - Debt/Equity   | 1.2        | [+ Add note]
  - NAV           | 182        | [+ Add note]
  - DPS           | 3.50       | [+ Add note]

Step 4 — Insights:
- Section heading "Insights (auto-detected)"
- One insight row with a warning icon: "Operating cash flow is negative despite positive net income"

Footer actions (right-aligned):
- [Discard] button (outlined, muted)
- [Save & Confirm] button (solid, dark navy, primary)
```

---

## Screen 6 — Scraper Control

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
