const { test, expect } = require('@playwright/test');

test.skip(!process.env.DOTTIN_STRIPE_QA_CPF || !process.env.DOTTIN_STRIPE_QA_PASSWORD,
  'Set synthetic paid-owner credentials to verify the Admin billing UI.');

test('proprietário pago vê o plano ativo e abre o portal Stripe', async ({ page }) => {
  test.setTimeout(60_000);
  await page.goto('/');
  await page.getByLabel('CPF').fill(process.env.DOTTIN_STRIPE_QA_CPF);
  await page.getByLabel('Senha').fill(process.env.DOTTIN_STRIPE_QA_PASSWORD);
  await page.getByLabel('Senha').press('Tab');
  await page.getByRole('button', { name: /Acessar painel/i }).click();
  await expect(page).toHaveURL(/\/dashboard/);

  if (process.env.DOTTIN_STRIPE_QA_SESSION_ID) {
    await page.goto(`/billing/success?session_id=${process.env.DOTTIN_STRIPE_QA_SESSION_ID}`);
    await expect(page.getByText('Pagamento recebido')).toBeVisible();
    await page.getByText('Ir para o painel').click();
    await expect(page).toHaveURL(/\/dashboard/);
  }
  await page.goto('/billing');
  await expect(page.getByText('Plano atual', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Professional', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Ativa', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Gerenciar cobrança' }).click();
  await expect(page).toHaveURL(/^https:\/\/billing\.stripe\.com\//);
});
