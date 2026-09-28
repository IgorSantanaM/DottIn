const { test, expect } = require('@playwright/test');

const credentials = {
  owner: { cpf: '10020030088', password: '123456' },
  administrator: { cpf: '22233344405', password: '123456' },
  employee: { cpf: '11122233396', password: '123456' }
};

async function login(page, account) {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Bem-vindo de volta' })).toBeVisible();

  const cpf = page.getByLabel('CPF');
  await cpf.fill(account.cpf);
  await expect(cpf).toHaveValue(/\d{3}\.\d{3}\.\d{3}-\d{2}/);
  await page.getByLabel('Senha').fill(account.password);

  await Promise.all([
    page.waitForURL(/\/dashboard$/),
    page.getByRole('button', { name: /Acessar painel/i }).click()
  ]);
  await expect(page.locator('.mud-main-content')).toBeVisible();
}

async function setAugustRangeAndSearch(page, expectedPath) {
  const start = page.getByLabel('Data Início');
  const end = page.getByLabel('Data Fim');
  await start.click();
  await page.getByRole('button', { name: 'Previous month (August 2026)' }).click();
  await page.locator('.mud-picker-calendar-day:not(.mud-hidden)').filter({ hasText: /^1$/ }).click();
  await expect(start).toHaveValue('01/08/2026');

  await end.click();
  await page.getByRole('button', { name: 'Previous month (August 2026)' }).click();
  await page.locator('.mud-picker-calendar-day:not(.mud-hidden)').filter({ hasText: /^31$/ }).click();
  await expect(end).toHaveValue('31/08/2026');

  const responsePromise = page.waitForResponse(response =>
    response.url().includes(expectedPath) &&
    response.url().includes('startDate=2026-08-01') &&
    response.url().includes('endDate=2026-08-31'));
  await page.getByRole('button', { name: 'Buscar' }).click();
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  return response.json();
}

test('owner: login, layout, consultas, páginas e persistência após refresh', async ({ page }) => {
  const apiErrors = [];
  page.on('response', response => {
    if (response.url().includes('/api/') && response.status() >= 400)
      apiErrors.push(`${response.status()} ${response.url()}`);
  });

  await login(page, credentials.owner);
  await expect(page.getByRole('heading', { name: 'Dashboard' })).toBeVisible();
  await expect(page.getByText('Funcionários Ativos')).toBeVisible();
  await expect(page.getByText('Ponto Compartilhado')).toBeVisible();

  const appBar = await page.locator('.mud-appbar').boundingBox();
  const dashboardTitle = await page.getByRole('heading', { name: 'Dashboard' }).boundingBox();
  expect(appBar).not.toBeNull();
  expect(dashboardTitle).not.toBeNull();
  expect(dashboardTitle.y).toBeGreaterThanOrEqual(appBar.y + appBar.height);

  await page.getByRole('link', { name: 'Registros' }).click();
  await expect(page.getByRole('heading', { name: 'Registros de Ponto' })).toBeVisible();
  await expect(page.getByText('Histórico de registros de ponto da empresa')).toBeVisible();
  const branchPage = await setAugustRangeAndSearch(page, '/api/timekeeping/branch/');
  expect(branchPage.totalCount).toBe(81);
  expect(branchPage.items.length).toBeGreaterThan(0);

  await page.reload();
  await expect(page).toHaveURL(/\/timekeeping$/);
  await expect(page.getByRole('heading', { name: 'Registros de Ponto' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Bem-vindo de volta' })).toHaveCount(0);

  await page.getByRole('link', { name: 'Funcionários' }).click();
  await expect(page.getByRole('heading', { name: 'Funcionários', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Feriados' }).click();
  await expect(page.getByRole('heading', { name: 'Calendário de Feriados' })).toBeVisible();
  await page.getByRole('link', { name: 'Assinatura' }).click();
  await expect(page.getByRole('heading', { name: 'Assinatura', exact: true })).toBeVisible();

  expect(apiErrors).toEqual([]);
});

test('administrator: mantém visão da filial depois de recarregar', async ({ page }) => {
  const historyResponses = [];
  page.on('response', response => {
    if (response.url().includes('/history/paged'))
      historyResponses.push({ status: response.status(), url: response.url() });
  });

  await login(page, credentials.administrator);
  await expect(page.getByRole('heading', { name: /Olá, Pedro!/ })).toBeVisible();
  await page.getByRole('link', { name: 'Registros' }).click();
  await expect(page.getByText('Histórico de registros de ponto da empresa')).toBeVisible();
  const branchPage = await setAugustRangeAndSearch(page, '/api/timekeeping/branch/');
  expect(branchPage.totalCount).toBe(81);

  await page.reload();
  await expect(page).toHaveURL(/\/timekeeping$/);
  await expect(page.getByText('Histórico de registros de ponto da empresa')).toBeVisible();
  expect(historyResponses.some(x => x.url.includes('/api/timekeeping/employee/'))).toBe(false);
  expect(historyResponses.every(x => x.status === 200)).toBe(true);
});
test('employee: consulta somente o próprio histórico e mantém sessão', async ({ page }) => {
  const apiResponses = [];
  page.on('response', response => {
    if (response.url().includes('/api/timekeeping/'))
      apiResponses.push({ status: response.status(), url: response.url() });
  });

  await login(page, credentials.employee);
  await expect(page.getByRole('heading', { name: /Olá, Joao!/ })).toBeVisible();

  await page.getByRole('link', { name: 'Registros' }).click();
  await expect(page.getByRole('heading', { name: 'Registros de Ponto' })).toBeVisible();
  await expect(page.getByText('Seu histórico de registros de ponto')).toBeVisible();
  const personalPage = await setAugustRangeAndSearch(page, '/api/timekeeping/employee/');
  expect(personalPage.totalCount).toBe(19);
  expect(personalPage.items.length).toBeGreaterThan(0);
  await expect(page.getByRole('columnheader', { name: 'Funcionário' })).toHaveCount(0);

  expect(apiResponses.some(x => x.url.includes('/api/timekeeping/branch/'))).toBe(false);
  expect(apiResponses.filter(x => x.url.includes('/history/paged')).every(x => x.status === 200)).toBe(true);

  await page.reload();
  await expect(page).toHaveURL(/\/timekeeping$/);
  await expect(page.getByText('Seu histórico de registros de ponto')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Bem-vindo de volta' })).toHaveCount(0);
});
