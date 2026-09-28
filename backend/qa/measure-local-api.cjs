// Local development baseline only; this is not a production load test.
const { performance } = require('node:perf_hooks');

const baseUrl = new URL(process.env.DOTTIN_QA_API_URL || 'http://localhost:5101');
const allowedHosts = new Set(['localhost', '127.0.0.1', '[::1]']);
if (!allowedHosts.has(baseUrl.hostname) || !['http:', 'https:'].includes(baseUrl.protocol))
  throw new Error('This benchmark accepts only a localhost API URL.');

const cpf = process.env.DOTTIN_QA_CPF;
const password = process.env.DOTTIN_QA_PASSWORD;
if (!cpf || !password)
  throw new Error('Set DOTTIN_QA_CPF and DOTTIN_QA_PASSWORD for a synthetic local account.');

function sampleCount(name, fallback, max) {
  const value = Number(process.env[name] || fallback);
  if (!Number.isInteger(value) || value < 1 || value > max)
    throw new Error(`${name} must be an integer between 1 and ${max}.`);
  return value;
}

const loginSamples = sampleCount('DOTTIN_QA_LOGIN_SAMPLES', 5, 10);
const readSamples = sampleCount('DOTTIN_QA_READ_SAMPLES', 15, 50);
const results = new Map();

async function measured(name, path, options = {}) {
  const started = performance.now();
  const response = await fetch(new URL(path, baseUrl), {
    ...options,
    signal: AbortSignal.timeout(15_000)
  });
  const body = await response.arrayBuffer();
  const elapsedMs = performance.now() - started;
  if (!response.ok)
    throw new Error(`${name} returned HTTP ${response.status}; benchmark aborted.`);
  if (!results.has(name)) results.set(name, []);
  results.get(name).push(elapsedMs);
  return { body, bytes: body.byteLength };
}

function summarize(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const percentile = p => sorted[Math.ceil((p / 100) * sorted.length) - 1];
  return {
    samples: sorted.length,
    p50Ms: Math.round(percentile(50) * 10) / 10,
    p95Ms: Math.round(percentile(95) * 10) / 10,
    maxMs: Math.round(sorted.at(-1) * 10) / 10
  };
}

(async () => {
  const live = await fetch(new URL('/health/ready', baseUrl), { signal: AbortSignal.timeout(5_000) });
  if (!live.ok) throw new Error('Local API is not ready.');

  let session;
  for (let i = 0; i < loginSamples; i += 1) {
    const response = await measured('login', '/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ cpf, password })
    });
    session = JSON.parse(Buffer.from(response.body).toString('utf8'));
    if (!session.accessToken || !session.branchId)
      throw new Error('Login response lacked branch context.');
  }

  const headers = { Authorization: `Bearer ${session.accessToken}` };
  const branchId = session.branchId;
  const history = `/api/timekeeping/branch/${branchId}/history/paged?startDate=2026-08-01&endDate=2026-08-31&pageNumber=1&pageSize=25`;
  const csv = `/api/branches/${branchId}/exports/csv?startDate=2026-08-01&endDate=2026-08-31`;
  let csvBytes = 0;
  for (let i = 0; i < readSamples; i += 1) {
    await measured('dashboard', `/api/branches/${branchId}/dashboard`, { headers });
    await measured('historyPaged', history, { headers });
    const exportResult = await measured('csvExport', csv, { headers });
    csvBytes = exportResult.bytes;
  }

  console.log(JSON.stringify({
    scope: 'localhost-development-baseline',
    concurrency: 1,
    csvBytes,
    metrics: Object.fromEntries([...results].map(([name, values]) => [name, summarize(values)]))
  }, null, 2));
})().catch(error => {
  console.error(error.message);
  process.exitCode = 1;
});
