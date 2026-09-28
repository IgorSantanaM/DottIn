const { test, expect } = require('@playwright/test');

test.skip(!process.env.DOTTIN_STRIPE_QA_CHECKOUT_URL, 'Set a test-mode Checkout URL to complete the payment.');

test('pagamento de teste conclui o Checkout Stripe', async ({ page }) => {
  test.setTimeout(90_000);
  await page.goto(process.env.DOTTIN_STRIPE_QA_CHECKOUT_URL);
  await expect(page.locator('input[name="cardNumber"]')).toBeVisible();
  await page.locator('input[name="cardNumber"]').fill('4242424242424242');
  await page.locator('input[name="cardExpiry"]').fill('1234');
  await page.locator('input[name="cardCvc"]').fill('567');
  await page.locator('input[name="billingName"]').fill('Proprietario Teste Stripe');
  await page.locator('input[name="billingAddressLine1"]').fill('Avenida Paulista 1000');
  await page.locator('input[name="billingLocality"]').fill('Sao Paulo');
  await page.locator('select[name="billingAdministrativeArea"]').selectOption({ label: 'São Paulo' });
  await page.locator('input[name="billingPostalCode"]').fill('01310000');
  await page.getByRole('button', { name: /^Subscribe/ }).click();
  await expect(page).not.toHaveURL(/^https:\/\/checkout\.stripe\.com\//, { timeout: 60_000 });
  console.log(`STRIPE_QA_RETURN_HOST=${new URL(page.url()).hostname}`);
});
