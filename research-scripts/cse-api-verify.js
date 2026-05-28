// Verifies working CSE API endpoints and period values.
// Run: npm install playwright && npx playwright install chromium && node cse-api-verify.js
//
// Key findings:
//   - Use application/x-www-form-urlencoded (NOT application/json) — JSON gives 400
//   - No authentication required
//   - period=5 returns 1 year of daily OHLCV data (~241 records)
//   - financials endpoint returns annual report PDF paths back to 2018

const { chromium } = require('playwright');

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

  // period values: 1=intraday, 3=1month daily, 5=1year daily, 2/7+=weekly
  console.log('=== Testing period values for companyChartDataByStock (JKH id=297) ===');
  for (const period of ['1', '2', '3', '5', '7', '30', '90', '180', '365', '1Y', '5Y', 'ALL']) {
    const r = await post('companyChartDataByStock', { stockId: '297', period });
    let info = r.status.toString();
    if (r.status === 200 && r.body.length > 5) {
      try {
        const data = JSON.parse(r.body);
        const chart = data.chartData || data;
        if (Array.isArray(chart)) {
          const first = chart[0];
          const last = chart[chart.length - 1];
          info = `${chart.length} records | first=${new Date(first?.t).toISOString().split('T')[0]} | last=${new Date(last?.t).toISOString().split('T')[0]}`;
        } else {
          info = r.body.substring(0, 100);
        }
      } catch(e) { info = r.body.substring(0, 100); }
    }
    console.log(`period="${period}" -> ${info}`);
  }

  // Annual report PDF list for JKH (symbol=JKH.N0000)
  // CDN base: https://cdn.cse.lk/{path}
  console.log('\n=== financials endpoint — annual report PDF list for JKH ===');
  const fin = await post('financials', { symbol: 'JKH.N0000' });
  console.log(fin.body.substring(0, 2000));

  await browser.close();
})();
