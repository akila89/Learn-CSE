// Verifies what the CSE financials endpoint returns for annual AND quarterly reports.
// Logs the full raw response and all top-level keys.
//
// Run: node research-scripts/cse-financials-verify.js

const { chromium } = require('playwright');

const SYMBOLS = ['JKH.N0000', 'COMB.N0000', 'LOLC.N0000'];

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  await page.goto('https://www.cse.lk', { waitUntil: 'networkidle', timeout: 30000 });

  const post = async (endpoint, fields) => {
    const form = new URLSearchParams(fields);
    const r = await context.request.post(`https://www.cse.lk/api/${endpoint}`, {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'Referer': 'https://www.cse.lk/' },
      data: form.toString()
    });
    return { status: r.status(), body: await r.text() };
  };

  for (const symbol of SYMBOLS) {
    console.log(`\n${'='.repeat(60)}`);
    console.log(`financials endpoint — ${symbol}`);
    console.log('='.repeat(60));

    const fin = await post('financials', { symbol });
    console.log(`HTTP status: ${fin.status}`);

    if (fin.status !== 200) {
      console.log('FAILED — non-200 response');
      console.log(fin.body.substring(0, 500));
      continue;
    }

    let parsed;
    try {
      parsed = JSON.parse(fin.body);
    } catch (e) {
      console.log('FAILED — could not parse JSON:', e.message);
      console.log(fin.body.substring(0, 500));
      continue;
    }

    console.log('\nTop-level keys:', Object.keys(parsed));

    for (const [key, value] of Object.entries(parsed)) {
      if (Array.isArray(value)) {
        console.log(`\n[${key}] — ${value.length} items`);
        // Show first 3 and last 1 to see format without flooding
        const sample = value.length <= 4 ? value : [...value.slice(0, 3), '...', value[value.length - 1]];
        console.log(JSON.stringify(sample, null, 2));
      } else {
        console.log(`\n[${key}]:`, JSON.stringify(value));
      }
    }
  }

  await browser.close();
})();
