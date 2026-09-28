const { test, expect } = require('@playwright/test');

const apiUrl = 'http://localhost:5101';
const owners = [
  { cpf: '10020030088', password: '123456' },
  { cpf: '33344455508', password: '123456' }
];

async function signIn(request, account) {
  const response = await request.post(`${apiUrl}/api/auth/login`, { data: account });
  expect(response.status()).toBe(200);
  const session = await response.json();
  expect(session.accessToken).toBeTruthy();
  expect(session.branchId).toBeTruthy();
  return session;
}

test('API: proprietários não acessam filial, pessoas, ponto ou exportações da outra empresa', async ({ request }) => {
  const [first, second] = await Promise.all(owners.map(owner => signIn(request, owner)));
  expect(first.branchId).not.toBe(second.branchId);

  for (const [actor, foreign] of [[first, second], [second, first]]) {
    const headers = { Authorization: `Bearer ${actor.accessToken}` };
    const ownBranch = await request.get(`${apiUrl}/api/branches/${actor.branchId}`, { headers });
    expect(ownBranch.status()).toBe(200);

    const paths = [
      `/api/branches/${foreign.branchId}`,
      `/api/branches/${foreign.branchId}/employees/`,
      `/api/branches/${foreign.branchId}/dominio-mappings`,
      `/api/branches/${foreign.branchId}/employee-invitations/`,
      `/api/branches/${foreign.branchId}/timekeeping-adjustments/`,
      `/api/branches/${foreign.branchId}/holiday-calendars/`,
      `/api/timekeeping/branch/${foreign.branchId}/history/paged?startDate=2026-08-01&endDate=2026-08-31&pageNumber=1&pageSize=25`,
      `/api/branches/${foreign.branchId}/exports/csv?startDate=2026-08-01&endDate=2026-08-31`
    ];
    for (const path of paths) {
      const response = await request.get(`${apiUrl}${path}`, { headers });
      expect(response.status(), `${path} should reject a foreign tenant`).toBe(403);
    }
  }
});

test('API: refresh token gira e logout revoga a sessão', async ({ request }) => {
  const original = await signIn(request, owners[1]);
  const firstRefresh = await request.post(`${apiUrl}/api/auth/refresh`, {
    data: { refreshToken: original.refreshToken }
  });
  expect(firstRefresh.status()).toBe(200);
  const rotated = await firstRefresh.json();
  expect(rotated.refreshToken).toBeTruthy();
  expect(rotated.refreshToken).not.toBe(original.refreshToken);

  const replay = await request.post(`${apiUrl}/api/auth/refresh`, {
    data: { refreshToken: original.refreshToken }
  });
  expect(replay.status()).toBe(401);

  for (const accessToken of [original.accessToken, rotated.accessToken]) {
    const validSession = await request.get(`${apiUrl}/api/branches/${original.branchId}`, {
      headers: { Authorization: `Bearer ${accessToken}` }
    });
    expect(validSession.status()).toBe(200);
  }

  const logout = await request.post(`${apiUrl}/api/auth/logout`, {
    headers: { Authorization: `Bearer ${rotated.accessToken}` }
  });
  expect(logout.status()).toBe(204);

  for (const accessToken of [original.accessToken, rotated.accessToken]) {
    const revokedSession = await request.get(`${apiUrl}/api/branches/${original.branchId}`, {
      headers: { Authorization: `Bearer ${accessToken}` }
    });
    expect(revokedSession.status()).toBe(401);
  }

  const afterLogout = await request.post(`${apiUrl}/api/auth/refresh`, {
    data: { refreshToken: rotated.refreshToken }
  });
  expect(afterLogout.status()).toBe(401);
});