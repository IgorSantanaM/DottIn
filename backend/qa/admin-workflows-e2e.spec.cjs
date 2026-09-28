const { test, expect } = require('@playwright/test');
const fs = require('node:fs/promises');

const adminUrl = 'http://localhost:5231';
const apiUrl = 'http://localhost:5101';
const branchId = 'a1b2c3d4-e5f6-7890-abcd-ef1234567890';

const credentials = {
  owner: { cpf: '10020030088', password: '123456' },
  employee: { cpf: '55566677720', password: '123456' }
};

async function login(page, account) {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Bem-vindo de volta' })).toBeVisible();
  await page.getByLabel('CPF').fill(account.cpf);
  await page.getByLabel('Senha').fill(account.password);
  await Promise.all([
    page.waitForURL(/\/dashboard$/),
    page.getByRole('button', { name: /Acessar painel/i }).click()
  ]);
  await expect(page.locator('.mud-main-content')).toBeVisible();
}

async function setAugustRangeAndSearch(page) {
  const start = page.getByLabel('Data Início');
  const end = page.getByLabel('Data Fim');

  await start.click();
  await page.getByRole('button', { name: 'Previous month (August 2026)' }).click();
  await page.locator('.mud-picker-calendar-day:not(.mud-hidden)').filter({ hasText: /^1$/ }).click();

  await end.click();
  await page.getByRole('button', { name: 'Previous month (August 2026)' }).click();
  await page.locator('.mud-picker-calendar-day:not(.mud-hidden)').filter({ hasText: /^31$/ }).click();

  const responsePromise = page.waitForResponse(response =>
    response.url().includes(`/api/timekeeping/branch/${branchId}/history/paged`) &&
    response.url().includes('startDate=2026-08-01') &&
    response.url().includes('endDate=2026-08-31'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  expect((await responsePromise).status()).toBe(200);
}

async function clickPunchAction(page, name, path, allowedStatuses = [200, 201, 204]) {
  const button = page.getByRole('button', { name, exact: true });
  if (!await button.waitFor({ state: 'visible', timeout: 3_000 }).then(() => true).catch(() => false))
    return null;

  const responsePromise = page.waitForResponse(response =>
    response.url().includes(path) && response.request().method() === 'POST');
  await button.click();
  const response = await responsePromise;
  expect(allowedStatuses).toContain(response.status());
  return response.status();
}

test('employee: registra ou verifica jornada web com geolocalização e auditoria de origem', async ({ page, context }) => {
  await context.grantPermissions(['geolocation'], { origin: adminUrl });
  await context.setGeolocation({ latitude: -23.5613, longitude: -46.6560, accuracy: 12 });

  const apiErrors = [];
  page.on('response', response => {
    const expectedBreakRejection = response.url().includes('/api/timekeeping/break') && response.status() === 422;
    if (response.url().includes('/api/') && response.status() >= 400 && !expectedBreakRejection)
      apiErrors.push(`${response.status()} ${response.url()}`);
  });

  await login(page, credentials.employee);
  await expect(page.getByRole('heading', { name: /Olá, Maria!/ })).toBeVisible();
  // The initial NotClocked view can appear before today's record has loaded.
  await expect(page.getByRole('button', { name: 'Atualizar' })).toBeEnabled();

  if (await page.getByRole('heading', { name: 'Jornada Finalizada' }).count() === 0) {
    await clickPunchAction(page, 'Registrar Entrada', '/api/timekeeping/clock-in');
    const breakStatus = await clickPunchAction(
      page, 'Iniciar Intervalo', '/api/timekeeping/break', [204, 422]);
    if (breakStatus === 204)
      await clickPunchAction(page, 'Retomar Trabalho', '/api/timekeeping/break', [204]);
    await clickPunchAction(page, 'Registrar Saída', '/api/timekeeping/clock-out');
  }

  await expect(page.getByRole('heading', { name: 'Jornada Finalizada' })).toBeVisible();
  await page.getByRole('link', { name: 'Registros' }).click();
  await expect(page.getByText('Seu histórico de registros de ponto')).toBeVisible();
  await expect(page.getByText('22/09/2026')).toBeVisible();
  await expect(page.getByText('Website', { exact: true }).first()).toBeVisible();

  expect(apiErrors).toEqual([]);
});

test('owner: configura Domínio, baixa CSV/TXT e valida integração de cobrança', async ({ page, request }) => {
  const apiErrors = [];
  page.on('response', response => {
    if (response.url().includes('/api/') && response.status() >= 400)
      apiErrors.push(`${response.status()} ${response.url()}`);
  });

  await login(page, credentials.owner);

  await page.getByRole('link', { name: 'Funcionários' }).click();
  await page.getByRole('button', { name: 'Mapeamento Domínio' }).click();
  const mappingDialog = page.getByRole('dialog');
  await expect(mappingDialog.getByRole('heading', { name: 'Mapeamento Domínio' })).toBeVisible();
  const rows = mappingDialog.locator('tbody tr');
  await expect(rows).toHaveCount(5);
  for (let index = 0; index < 5; index += 1)
    await rows.nth(index).locator('input').fill(String(index + 1));

  const saveMappings = page.waitForResponse(response =>
    response.url().includes(`/api/branches/${branchId}/dominio-mappings`) &&
    response.request().method() === 'PUT');
  await mappingDialog.getByRole('button', { name: 'Salvar' }).click();
  expect((await saveMappings).status()).toBe(204);

  await page.getByRole('link', { name: 'Registros' }).click();
  await setAugustRangeAndSearch(page);

  await page.locator('button.mud-button-outlined-default.mud-menu-icon-button-activator').click();
  const csvDownloadPromise = page.waitForEvent('download');
  await page.getByText('Exportar CSV (.csv)', { exact: true }).click();
  const csvDownload = await csvDownloadPromise;
  expect(csvDownload.suggestedFilename()).toBe('registros_20260801_20260831.csv');
  const csv = await fs.readFile(await csvDownload.path(), 'utf8');
  expect(csv).toContain('Funcionário,Data,Entrada,Saída');
  expect(csv).toContain('Joao Silva');
  expect(csv.split(/\r?\n/).length).toBeGreaterThan(50);

  await page.locator('button.mud-button-outlined-default.mud-menu-icon-button-activator').click();
  await page.getByText('Exportar Domínio (.txt)', { exact: true }).click();
  const exportDialog = page.getByRole('dialog');
  await expect(exportDialog.getByRole('heading', { name: 'Exportar Domínio' })).toBeVisible();
  const month = exportDialog.getByLabel('Mês de Referência');
  await month.click();
  const august = page.locator('.mud-picker-month').filter({ hasText: /^(Aug|ago)\.?$/i });
  await expect(august).toBeVisible();
  await august.click();
  await page.locator('.mud-picker-calendar-day:not(.mud-hidden)').filter({ hasText: /^1$/ }).click();
  await expect(month).toHaveValue('08/2026');
  const companyCode = exportDialog.getByLabel('Código da empresa no Domínio');
  await companyCode.fill('1');
  await companyCode.press('Tab');
  await expect(exportDialog.getByRole('button', { name: 'Exportar', exact: true })).toBeEnabled();

  const dominioResponsePromise = page.waitForResponse(response =>
    response.url().includes(`/api/branches/${branchId}/exports/dominio`) &&
    response.url().includes('month=2026-08'));
  const dominioDownloadPromise = page.waitForEvent('download');
  await exportDialog.getByRole('button', { name: 'Exportar', exact: true }).click();
  expect((await dominioResponsePromise).status()).toBe(200);
  const dominioDownload = await dominioDownloadPromise;
  expect(dominioDownload.suggestedFilename()).toBe('dominio_202608.txt');
  const dominio = await fs.readFile(await dominioDownload.path(), 'utf8');
  const lines = dominio.trim().split(/\r?\n/);
  expect(lines.length).toBeGreaterThan(0);
  expect(lines.every(line => line.length > 30)).toBe(true);

  const billingResponses = [];
  page.on('response', response => {
    if (response.url().includes('/api/billing/'))
      billingResponses.push({ url: response.url(), status: response.status() });
  });
  await page.getByRole('link', { name: 'Assinatura' }).click();
  await expect(page.getByRole('heading', { name: 'Assinatura', exact: true })).toBeVisible();
  await expect.poll(() => billingResponses.length).toBeGreaterThanOrEqual(2);
  expect(billingResponses.every(response => response.status === 200)).toBe(true);

  const unsignedWebhook = await request.post(`${apiUrl}/api/webhooks/stripe`, {
    data: { id: 'evt_e2e_unsigned', type: 'ping' }
  });
  expect(unsignedWebhook.status()).toBe(400);

  expect(apiErrors).toEqual([]);
});
