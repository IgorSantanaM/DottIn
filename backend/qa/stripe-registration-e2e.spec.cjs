const { test, expect } = require('@playwright/test');

// Opt-in because each run creates a local owner/branch and a Stripe test customer.
test.skip(!process.env.DOTTIN_STRIPE_E2E, 'Set DOTTIN_STRIPE_E2E=1 for a new Stripe test registration.');

function checkDigits(base, weights) {
  const sum = [...base].reduce((total, digit, index) => total + Number(digit) * weights[index], 0);
  const remainder = sum % 11;
  return remainder < 2 ? 0 : 11 - remainder;
}

function cpfFrom(base) {
  const first = checkDigits(base, [10, 9, 8, 7, 6, 5, 4, 3, 2]);
  const second = checkDigits(`${base}${first}`, [11, 10, 9, 8, 7, 6, 5, 4, 3, 2]);
  return `${base}${first}${second}`;
}

function cnpjFrom(base) {
  const first = checkDigits(base, [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  const second = checkDigits(`${base}${first}`, [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2]);
  return `${base}${first}${second}`;
}

test('novo proprietário cria filial, cliente Stripe de teste e checkout Professional', async ({ page, context }) => {
  test.setTimeout(120_000);
  const runId = process.env.DOTTIN_STRIPE_QA_RUN_ID || Date.now().toString();
  const resume = Boolean(process.env.DOTTIN_STRIPE_QA_RUN_ID);
  const cpf = cpfFrom(`713${runId.slice(-6)}`);
  const cnpj = cnpjFrom(`42${runId.slice(-10)}`);
  const email = `qa-stripe-${runId}@example.com`;
  const companyName = `DottIn QA Stripe ${runId}`;
  const password = `QA!${runId}aA`;

  await context.grantPermissions(['geolocation'], { origin: 'http://localhost:5231' });
  await context.setGeolocation({ latitude: -23.5613, longitude: -46.6560, accuracy: 12 });

  let owner;
  if (resume) {
    await page.goto('/');
    await page.getByLabel('CPF').fill(cpf);
    await page.getByLabel('Senha').fill(password);
    await page.getByLabel('Senha').press('Tab');
    const loginResponsePromise = page.waitForResponse(response =>
      response.url().endsWith('/api/auth/login') && response.request().method() === 'POST');
    await page.getByRole('button', { name: /Acessar painel/i }).click();
    const loginResponse = await loginResponsePromise;
    expect(loginResponse.status(), await loginResponse.text()).toBe(200);
    owner = await loginResponse.json();
    await page.goto('/onboarding/plan');
  } else {
    await page.goto('/register');
    await expect(page.getByRole('heading', { name: 'Crie sua conta' })).toBeVisible();
    await page.getByLabel('Nome Completo').fill('Proprietario Teste Stripe');
    await page.getByLabel('CPF').fill(cpf);
    await page.getByLabel('Senha', { exact: true }).fill(password);
    await page.getByLabel('Confirmar Senha').fill(password);
    await page.getByLabel('Confirmar Senha').press('Tab');
    await expect(page.getByRole('button', { name: 'Criar Conta' })).toBeEnabled();

    const ownerResponsePromise = page.waitForResponse(response =>
      response.url().endsWith('/api/auth/register/owner') && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Criar Conta' }).click();
    const ownerResponse = await ownerResponsePromise;
    expect(ownerResponse.status(), await ownerResponse.text()).toBe(201);
    owner = await ownerResponse.json();
    await expect(page).toHaveURL(/\/onboarding\/plan$/);
  }
  await expect(page.getByRole('heading', { name: 'Professional' })).toBeVisible();
  await page.getByRole('button', { name: 'Selecionar Professional' }).click();
  await expect(page).toHaveURL(/\/onboarding\/branch\?planId=/);

  await page.getByLabel('Nome da Filial').fill(companyName);
  await page.getByLabel('E-mail Organizacional').fill(email);
  await page.getByLabel('Celular / Telefone').fill('11987654321');
  await page.getByLabel('Documento').fill(cnpj);
  await page.getByLabel('CEP').fill('01310000');
  await page.getByLabel('Logradouro (Rua/Av.)').fill('Avenida Paulista');
  await page.getByLabel('Número').fill('1000');
  await page.getByLabel('Cidade').fill('Sao Paulo');
  await page.getByLabel('UF').click();
  await page.getByText('SP', { exact: true }).click();

  const branchResponsePromise = page.waitForResponse(response =>
    response.url().endsWith('/api/branches') && response.request().method() === 'POST');
  const checkoutResponsePromise = page.waitForResponse(response =>
    response.url().endsWith('/api/billing/checkout-session') && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Finalizar Registro' }).click();

  const branchResponse = await branchResponsePromise;
  expect(branchResponse.status(), await branchResponse.text()).toBe(201);
  const branch = await branchResponse.json();

  const checkoutResponse = await checkoutResponsePromise;
  expect(checkoutResponse.status(), await checkoutResponse.text()).toBe(200);
  const checkout = await checkoutResponse.json();
  expect(new URL(checkout.checkoutUrl).hostname).toBe('checkout.stripe.com');
  await expect(page).toHaveURL(/^https:\/\/checkout\.stripe\.com\//);

  const subscriptionResponse = await context.request.get('http://localhost:5101/api/billing/subscription', {
    headers: { Authorization: `Bearer ${owner.accessToken}` }
  });
  expect(subscriptionResponse.status()).toBe(200);
  const subscription = await subscriptionResponse.json();
  expect(subscription.planName).toBe('Free');

  console.log(`STRIPE_QA_OWNER_ID=${owner.employee.id}`);
  console.log(`STRIPE_QA_BRANCH_ID=${branch.branchId}`);
  console.log(`STRIPE_QA_EMAIL=${email}`);
});
