import assert from 'node:assert/strict';
import { writeFileSync } from 'node:fs';

const target = process.env.E2E_TARGET_URL?.trim();
const email = process.env.E2E_ADMIN_EMAIL?.trim();
const password = process.env.E2E_ADMIN_PASSWORD ?? '';
const evidencePath = process.env.E2E_EVIDENCE_OUT?.trim();

if (target === undefined || email === undefined || password === '') {
  throw new Error('E2E_TARGET_URL, E2E_ADMIN_EMAIL and E2E_ADMIN_PASSWORD are required.');
}

const origin = new URL(target);
const loopbackHosts = new Set(['localhost', '127.0.0.1', '::1']);
const isLocalHttp = origin.protocol === 'http:' && loopbackHosts.has(origin.hostname.toLowerCase());
assert.ok(origin.protocol === 'https:' || isLocalHttp, 'E2E requires HTTPS except on loopback.');
assert.equal(origin.username, '', 'Target URL must not contain credentials.');
assert.equal(origin.password, '', 'Target URL must not contain credentials.');

const cookies = new Map();

function captureCookies(response) {
  for (const header of response.headers.getSetCookie()) {
    const [pair, ...attributes] = header.split(';').map((part) => part.trim());
    const separator = pair.indexOf('=');
    const name = pair.slice(0, separator);
    const value = pair.slice(separator + 1);
    const maxAge = attributes.find((attribute) => attribute.toLowerCase().startsWith('max-age='));
    if (value === '' || maxAge?.toLowerCase() === 'max-age=0') {
      cookies.delete(name);
    } else {
      cookies.set(name, value);
    }
  }
}

function cookieHeader() {
  return [...cookies.entries()].map(([name, value]) => `${name}=${value}`).join('; ');
}

async function request(path, options = {}) {
  const headers = new Headers(options.headers);
  headers.set('Origin', origin.origin);
  if (cookies.size > 0) {
    headers.set('Cookie', cookieHeader());
  }
  const response = await fetch(new URL(path, origin), {
    ...options,
    headers,
    redirect: 'error',
  });
  captureCookies(response);
  return response;
}

const startedAt = new Date().toISOString();

const csrfResponse = await request('/api/v1/auth/csrf');
assert.equal(csrfResponse.status, 200, 'CSRF endpoint must respond 200.');
const csrfCookie = csrfResponse.headers
  .getSetCookie()
  .find((header) => header.startsWith('__Host-lu_csrf='));
assert.ok(csrfCookie, 'CSRF cookie is missing.');
assert.match(csrfCookie, /; HttpOnly/i);
assert.match(csrfCookie, /; Secure/i);
assert.match(csrfCookie, /; SameSite=Lax/i);
assert.match(csrfCookie, /; Path=\//i);
const csrfBody = await csrfResponse.json();
assert.equal(typeof csrfBody.csrfToken, 'string');
assert.ok(csrfBody.csrfToken.length >= 32);

const loginResponse = await request('/api/v1/auth/login', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'X-CSRF-Token': csrfBody.csrfToken,
  },
  body: JSON.stringify({ email, password, activeSiteId: null }),
});
assert.equal(loginResponse.status, 200, 'Login must respond 200.');
const sessionCookie = loginResponse.headers
  .getSetCookie()
  .find((header) => header.startsWith('__Host-lu_session='));
assert.ok(sessionCookie, 'Session cookie is missing.');
assert.match(sessionCookie, /; HttpOnly/i);
assert.match(sessionCookie, /; Secure/i);
assert.match(sessionCookie, /; SameSite=Lax/i);
assert.match(sessionCookie, /; Path=\//i);
const loginBody = await loginResponse.json();
assert.equal(loginBody.success, true);
assert.equal(loginBody.data.globalRole, 'SuperAdmin');
assert.equal(loginBody.data.memberships.length, 1);
assert.equal(loginBody.data.memberships[0].role, 'Administrador');
assert.equal(loginBody.data.activeSiteId, loginBody.data.memberships[0].siteId);

const sessionResponse = await request('/api/v1/auth/session');
assert.equal(sessionResponse.status, 200, 'Authenticated session must respond 200.');
const sessionBody = await sessionResponse.json();
assert.equal(sessionBody.success, true);
assert.equal(sessionBody.data.activeSiteId, loginBody.data.activeSiteId);

const contextResponse = await request('/api/v1/context');
assert.equal(contextResponse.status, 200, 'Site context must respond 200.');
const contextBody = await contextResponse.json();
assert.equal(contextBody.success, true);
assert.equal(contextBody.data.siteId, loginBody.data.activeSiteId);
assert.equal(contextBody.data.siteRole, 'Administrador');

const logoutResponse = await request('/api/v1/auth/logout', {
  method: 'POST',
  headers: { 'X-CSRF-Token': csrfBody.csrfToken },
});
assert.equal(logoutResponse.status, 204, 'Logout must respond 204.');

const revokedResponse = await request('/api/v1/auth/session');
assert.equal(revokedResponse.status, 401, 'Revoked session must respond 401.');

const evidence = {
  workId: 'QA-F4-DEPLOY-AUTH-006',
  target: origin.origin,
  startedAt,
  finishedAt: new Date().toISOString(),
  checks: {
    csrf: 'PASS',
    secureCookieAttributes: 'PASS',
    login: 'PASS',
    membership: 'PASS',
    session: 'PASS',
    siteContext: 'PASS',
    logout: 'PASS',
    revokedSessionRejected: 'PASS',
  },
  observed: {
    membershipCount: loginBody.data.memberships.length,
    role: contextBody.data.siteRole,
    activeSiteSelected: contextBody.data.siteId === loginBody.data.activeSiteId,
  },
  containsSecrets: false,
};

if (evidencePath !== undefined && evidencePath !== '') {
  writeFileSync(evidencePath, `${JSON.stringify(evidence, null, 2)}\n`, { encoding: 'utf8' });
}

console.log(JSON.stringify(evidence));
