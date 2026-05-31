// Discovers CSE API endpoints by intercepting real browser requests on the company profile page.
// Run: npm install playwright && npx playwright install chromium && node cse-api-discover.js
//
// This script navigates to a company profile page, clicks Chart and Financials tabs,
// and logs all API calls made — useful for finding new endpoints if the site changes.

const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext();
  const page = await context.newPage();

  page.on('request', request => {
    if (request.url().includes('/api/')) {
      console.log(`REQ [${request.method()}] ${request.url().split('/api/')[1]}`);
      if (request.postData()) console.log('  Body:', request.postData().substring(0, 200));
    }
  });

  page.on('response', async response => {
    if (response.url().includes('/api/') &&
        !response.url().includes('banner') &&
        !response.url().includes('notification') &&
        !response.url().includes('allSecurityCode') &&
        !response.url().includes('news')) {
      try {
        const body = await response.text();
        if (body && body.length > 5) {
          console.log(`RES [${response.status()}] ${response.url().split('/api/')[1]}: ${body.substring(0, 400)}\n`);
        }
      } catch(e) {}
    }
  });

  // Company profile page — change symbol to any tracked stock
  await page.goto('https://www.cse.lk/company-profile?symbol=JKH.N0000', {
    waitUntil: 'networkidle', timeout: 30000
  });
  await page.waitForTimeout(3000);

  // Click tabs to trigger additional API calls
  const tabs = await page.$$('button, a, [role="tab"]');
  console.log(`\nFound ${tabs.length} clickable elements`);

  for (const tab of tabs) {
    try {
      const text = await tab.textContent();
      if (text && (text.toLowerCase().includes('chart') ||
                   text.toLowerCase().includes('trade') ||
                   text.toLowerCase().includes('price') ||
                   text.toLowerCase().includes('financial'))) {
        console.log(`Clicking tab: "${text.trim()}"`);
        await tab.click();
        await page.waitForTimeout(3000);
      }
    } catch(e) {}
  }

  // Test companyChartDataByStock with form data — confirmed working format
  console.log('\n--- companyChartDataByStock with form data (confirmed working) ---');
  const formAttempts = [
    { stockId: '297', period: '5' },  // 1 year daily — use this for technical analysis
    { stockId: '297', period: '3' },  // 1 month daily
    { stockId: '297', period: '1' },  // intraday
  ];

  for (const fields of formAttempts) {
    const form = new URLSearchParams(fields);
    const r = await context.request.post('https://www.cse.lk/api/companyChartDataByStock', {
      headers: { 'Content-Type': 'application/x-www-form-urlencoded', 'Referer': 'https://www.cse.lk/' },
      data: form.toString()
    });
    const text = await r.text();
    const preview = text.length > 5 ? text.substring(0, 150) + '...' : '(empty)';
    console.log(`${JSON.stringify(fields)} -> ${r.status()}: ${preview}`);
  }

  await browser.close();
})();
