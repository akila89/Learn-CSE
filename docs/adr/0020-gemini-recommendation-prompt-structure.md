# Gemini recommendation prompt structure and output schema

The nightly Recommendation for each Stock is generated from a single Gemini 1.5 Flash call whose input is assembled entirely from data already in the database — no additional API calls are made during prompt construction. The prompt is built in the scraper after the price scrape and indicator calculation phases complete (see ADR 0006).

The prompt contains six sections in this order:

1. **Company Profile** — the structured description extracted from the most recent Annual Report (what the business does, sector, key revenue drivers, USD vs LKR orientation, state ownership if applicable). Sourced from the `companies` table.

2. **Fundamental indicators** — the latest values for all seven indicators (P/E, EPS, ROE, Revenue Growth YoY, Debt/Equity, Dividend Yield, NAV), each labelled with the source Report's period and audit status (Audited / Unaudited). Sourced from `company_fundamentals` and `stock_fundamentals`. _(v2: where a figure has an associated Annotation, the annotation text will be included inline — e.g. `EPS: 4.20 [User note: includes one-off gain from logistics division sale — not recurring]`. Annotations are deferred to v2.)_

3. **Insights** — the list of structured observations auto-generated during the last Report ingestion (e.g. "operating cash flow is negative despite positive net income"). Sourced from the `insights` column on `companies`. Included verbatim.

4. **Technical indicators** — the latest row from `daily_indicators`: RSI, MACD line and signal, SMA 50, SMA 200, Bollinger upper/mid/lower, 52-week high/low. Each value is labelled with its plain-English interpretation so Gemini does not need to infer it (e.g. `RSI: 28 — below 30, indicates oversold conditions`).

5. **Current price and recent trend** — the latest closing price and the percentage change over the past 30 days, derived from `daily_prices`.

6. **Output instruction** — explicit instruction to return a JSON object matching the schema below and nothing else.

The required output schema:

```json
{
  "signal": "BUY | ACCUMULATE | HOLD | REDUCE | SELL",
  "confidence": "HIGH | MEDIUM | LOW",
  "reasoning": "<3-5 sentence plain-English explanation referencing the specific figures above>",
  "risks": ["<risk 1>", "<risk 2>"]
}
```

The raw Gemini response (the full JSON string before any parsing) is stored in the `recommendations` table alongside the parsed fields. This mirrors the report extraction pattern (see ADR 0012) and allows the prompt to be improved and back-tested against historical raw outputs without re-calling Gemini. If Gemini returns malformed JSON or an unexpected signal value, the scraper logs the error, stores the raw response, and leaves the previous Recommendation as the current one — it does not overwrite a valid Recommendation with a parse failure.

Sequential rate limiting (4-second delay between stocks) is inherited from ADR 0009 and applies to Recommendation calls in the same queue as Report extraction calls.
