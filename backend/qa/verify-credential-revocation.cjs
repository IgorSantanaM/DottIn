// Invoked only by verify-credential-revocation.ps1 against its disposable database.
const assert = require('node:assert/strict');

if (process.env.DOTTIN_QA_CREDENTIAL_ISOLATED !== '1' ||
    process.env.DOTTIN_QA_API_URL !== 'http://127.0.0.1:5102') {
  throw new Error('Credential E2E requires the isolated localhost API on port 5102.');
}

const baseUrl = process.env.DOTTIN_QA_API_URL;
const cpf = '10020030088';
const originalPassword = '123456';
const newPassword = 'NovaSenha123!';

async function call(path, method, data, accessToken) {
  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      ...(data ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {})
    },
    body: data ? JSON.stringify(data) : undefined,
    signal: AbortSignal.timeout(15_000)
  });
  return response;
}

async function login(password) {
  const response = await call('/api/auth/login', 'POST', { cpf, password });
  assert.equal(response.status, 200, 'Password login should succeed');
  return response.json();
}

async function assertRevoked(session) {
  const access = await call(`/api/branches/${session.branchId}`, 'GET', null, session.accessToken);
  assert.equal(access.status, 401, 'Previously issued access token must be revoked');
  const refresh = await call('/api/auth/refresh', 'POST', { refreshToken: session.refreshToken });
  assert.equal(refresh.status, 401, 'Previously issued refresh token must be revoked');
}

(async () => {
  const initial = await login(originalPassword);
  const branchRead = await call(`/api/branches/${initial.branchId}`, 'GET', null, initial.accessToken);
  assert.equal(branchRead.status, 200);

  const weak = await call('/api/auth/change-password', 'PUT', {
    companyCode: initial.companyCode, cpf,
    currentPassword: originalPassword, newPassword: 'SemNumeroAqui!'
  }, initial.accessToken);
  assert.equal(weak.status, 422, 'Weak password must be rejected by the domain');
  const stillActive = await call(`/api/branches/${initial.branchId}`, 'GET', null, initial.accessToken);
  assert.equal(stillActive.status, 200, 'Rejected change must not revoke the session');

  const passwordChange = await call('/api/auth/change-password', 'PUT', {
    companyCode: initial.companyCode, cpf,
    currentPassword: originalPassword, newPassword
  }, initial.accessToken);
  assert.equal(passwordChange.status, 204, 'Strong password change should succeed');
  await assertRevoked(initial);
  const oldLogin = await call('/api/auth/login', 'POST', { cpf, password: originalPassword });
  assert.equal(oldLogin.status, 401, 'Old password must fail');

  const renewed = await login(newPassword);
  const pinChange = await call('/api/auth/change-pin', 'PUT', {
    companyCode: renewed.companyCode, cpf,
    currentPassword: newPassword, newPin: '654321'
  }, renewed.accessToken);
  assert.equal(pinChange.status, 204, 'PIN change should succeed');
  await assertRevoked(renewed);

  const pinLogin = await call('/api/auth/login/pin', 'POST', {
    cpf, pin: '654321', companyCode: renewed.companyCode
  });
  assert.equal(pinLogin.status, 200, 'New PIN login should succeed');
  console.log('Credential changes revoked access/refresh tokens; strong password and new PIN verified.');
})().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
