const { defineConfig } = require('@playwright/test');

module.exports = defineConfig({
  testDir: '.',
  testMatch: ['admin-e2e.spec.cjs', 'admin-workflows-e2e.spec.cjs', 'admin-tenant-security-e2e.spec.cjs', 'stripe-registration-e2e.spec.cjs', 'stripe-checkout-payment-e2e.spec.cjs', 'stripe-paid-admin-e2e.spec.cjs'],
  fullyParallel: false,
  workers: 1,
  timeout: 60_000,
  expect: { timeout: 15_000 },
  reporter: [['list']],
  use: {
    baseURL: 'http://localhost:5231',
    browserName: 'chromium',
    channel: 'chrome',
    headless: true,
    viewport: { width: 1440, height: 900 },
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure'
  }
});
