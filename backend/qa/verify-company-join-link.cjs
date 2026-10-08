// Run only with an isolated API/database through verify-company-join-link.ps1.
const assert = require('node:assert/strict');

if (process.env.DOTTIN_QA_JOIN_ISOLATED !== '1' ||
    process.env.DOTTIN_QA_API_URL !== 'http://localhost:5102') {
  throw new Error('Company join-link E2E requires the isolated localhost API on port 5102.');
}

const baseUrl = process.env.DOTTIN_QA_API_URL;
const adminUrl = process.env.DOTTIN_QA_ADMIN_URL;
if (adminUrl && !/^http:\/\/localhost:5\d{3}$/.test(adminUrl)) {
  throw new Error('Company join-link browser E2E requires a local Admin URL on port 5000-5999.');
}
const ownerCpf = '10020030088';
const otherOwnerCpf = '33344455508';
const newCpf = '52998224725';
const unassignedCpf = '24681357928';
const newPassword = 'ConviteTeste1!';

async function call(path, method = 'GET', data, accessToken) {
  return fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      ...(data ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
    },
    body: data ? JSON.stringify(data) : undefined,
    signal: AbortSignal.timeout(15_000)
  });
}

async function login(cpf, password, companyJoinToken) {
  const response = await call('/api/auth/login', 'POST', { cpf, password, companyJoinToken });
  return { response, session: response.ok ? await response.json() : null };
}

(async () => {
  const owner = await login(ownerCpf, '123456');
  assert.equal(owner.response.status, 200);
  const branchId = owner.session.branchId;

  const linkResponse = await call(`/api/branches/${branchId}/company-join-link`, 'GET', null, owner.session.accessToken);
  assert.equal(linkResponse.status, 200, 'Owner must be able to obtain a shareable branch link');
  const link = await linkResponse.json();
  assert.ok(link.token);

  const resolved = await call(`/api/company-join-links/resolve?token=${encodeURIComponent(link.token)}`);
  assert.equal(resolved.status, 200, 'Anonymous visitor must resolve the link');
  const company = await resolved.json();
  assert.equal(company.canJoin, true);

  const invalid = await call('/api/company-join-links/resolve?token=invalid');
  assert.equal(invalid.status, 400);

  const registered = await call('/api/company-join-links/register', 'POST', {
    token: link.token, name: 'Novo Funcionário Convite', cpf: newCpf, password: newPassword
  });
  assert.equal(registered.status, 201, 'New account must be created and joined in one action');
  const created = await registered.json();
  assert.equal(created.branchId, branchId);
  assert.ok(created.accessToken);
  const authorized = await call(`/api/branches/${branchId}`, 'GET', null, created.accessToken);
  assert.equal(authorized.status, 200, 'New employee must immediately have access to the invited branch');
  const employeeCannotShare = await call(`/api/branches/${branchId}/company-join-link`, 'GET', null, created.accessToken);
  assert.equal(employeeCannotShare.status, 403, 'Employees must not be able to issue company invitations');

  const newLogin = await login(newCpf, newPassword);
  assert.equal(newLogin.response.status, 200, 'New account must be able to sign in without another invitation');
  assert.equal(newLogin.session.branchId, branchId);
  assert.equal(newLogin.session.role, 'Employee', 'An invited account must have employee permissions');
  const alreadyJoined = await login(newCpf, newPassword, link.token);
  assert.equal(alreadyJoined.response.status, 200, 'An existing member can use the same company link to sign in');

  const duplicate = await call('/api/company-join-links/register', 'POST', {
    token: link.token, name: 'Duplicado', cpf: newCpf, password: newPassword
  });
  assert.equal(duplicate.status, 409, 'Existing CPF must not create a second account');

  const otherOwner = await login(otherOwnerCpf, '123456');
  assert.equal(otherOwner.response.status, 200);
  assert.notEqual(otherOwner.session.branchId, branchId);
  const wrongBranch = await login(otherOwnerCpf, '123456', link.token);
  assert.equal(wrongBranch.response.status, 409,
    'Existing account in another company must not silently ignore the invitation');

  const ownerRegistration = await call('/api/auth/register/owner', 'POST', {
    name: 'Conta Sem Filial', document: { value: unassignedCpf, type: 'CPF' }, password: newPassword
  });
  assert.equal(ownerRegistration.status, 201, 'Create an existing account without a branch');
  const unassignedSession = await ownerRegistration.json();
  const joinedExisting = await login(unassignedCpf, newPassword, link.token);
  assert.equal(joinedExisting.response.status, 200, 'An unassigned account must be able to accept the link');
  assert.equal(joinedExisting.session.branchId, branchId);
  const staleOwnerSession = await call(`/api/branches/${branchId}`, 'GET', null, unassignedSession.accessToken);
  assert.equal(staleOwnerSession.status, 401, 'Joining must revoke a previous unassigned owner access token');
  const existingCannotShare = await call(`/api/branches/${branchId}/company-join-link`, 'GET', null, joinedExisting.session.accessToken);
  assert.equal(existingCannotShare.status, 403, 'An invited former unassigned owner must not inherit owner permissions');

  if (adminUrl) {
    const { chromium, expect } = require('@playwright/test');
    const browser = await chromium.launch({ channel: 'chrome', headless: true });
    try {
      const ownerContext = await browser.newContext({ permissions: ['clipboard-read', 'clipboard-write'] });
      await ownerContext.route('**/appsettings.Development.json', route => route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({ ApiBaseUrl: baseUrl })
      }));
      const ownerPage = await ownerContext.newPage();
      await ownerPage.goto(`${adminUrl}/`);
      await expect(ownerPage.getByRole('heading', { name: 'Bem-vindo de volta' })).toBeVisible();
      await ownerPage.getByLabel('CPF').fill(ownerCpf);
      await ownerPage.getByLabel('Senha').fill('123456');
      await ownerPage.getByRole('button', { name: /Acessar painel/i }).click();
      await expect.poll(() => new URL(ownerPage.url()).pathname).toBe('/dashboard');
      await ownerPage.goto(`${adminUrl}/employees`);
      await ownerPage.getByRole('button', { name: 'Copiar convite' }).click();
      await expect(ownerPage.getByText('Convite copiado.')).toBeVisible();
      const sharedUrl = await ownerPage.evaluate(() => navigator.clipboard.readText());
      assert.ok(sharedUrl.startsWith(`${adminUrl}/join?token=`));
      await ownerContext.close();

      const context = await browser.newContext();
      await context.route('**/appsettings.Development.json', route => route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({ ApiBaseUrl: baseUrl })
      }));
      const page = await context.newPage();
      await page.goto(sharedUrl);
      await expect(page.getByText(`Entrar em ${company.companyName}`)).toBeVisible();
      await page.getByText('Criar conta', { exact: true }).first().click();
      await page.getByLabel('Nome completo').fill('Pessoa Convite Web');
      await page.getByLabel('CPF').last().fill('11144477735');
      await page.getByLabel('Senha').last().fill('ConviteWeb123!');
      await page.getByRole('button', { name: 'Criar conta e entrar' }).click();
      await expect.poll(() => new URL(page.url()).pathname, { timeout: 20_000 }).toBe('/dashboard');
      await expect(page.locator('.mud-appbar .mud-chip')).toHaveText('Funcionário');
      await page.locator('.mud-appbar button').last().click();
      await expect(page.getByText('Funcionário', { exact: true })).toHaveCount(2);
      await page.reload();
      await expect.poll(() => new URL(page.url()).pathname, { timeout: 20_000 }).toBe('/dashboard');
      await expect(page.locator('.mud-appbar .mud-chip')).toHaveText('Funcionário');
      await context.close();
    } finally {
      await browser.close();
    }
  }

  console.log('Owner link, resolution, auto-join, duplicate/cross-branch protection and optional browser flow verified.');
})().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
