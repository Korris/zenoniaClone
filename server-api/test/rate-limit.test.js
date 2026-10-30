import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { RateLimiter } from '../src/auth/rate-limiter.js';
import { makeCtx } from './helpers.js';

describe('RateLimiter', () => {
  it('allows max hits per window, then blocks until the window resets', () => {
    let t = 1000;
    const l = new RateLimiter({ max: 2, windowMs: 10_000, now: () => t });
    expect(l.hit('a').allowed).toBe(true);
    expect(l.hit('a').allowed).toBe(true);
    const blocked = l.hit('a');
    expect(blocked.allowed).toBe(false);
    expect(blocked.retryAfterSec).toBe(10);
    expect(l.hit('b').allowed).toBe(true); // keys are independent
    t += 10_001;
    expect(l.hit('a').allowed).toBe(true);
  });
});

describe('registration limit per IP', () => {
  let c;
  beforeAll(async () => { c = await makeCtx({ cfg: { REGISTER_IP_MAX: '3', REGISTER_IP_WINDOW_SEC: '3600' } }); });
  afterAll(() => c.close());

  it('blocks the 4th registration from one IP with 429 + Retry-After; other IPs unaffected; window expiry frees it', async () => {
    const r = (i, ip) => c.call('POST', '/auth/register', { ip, body: { username: `user_${i}`, email: `u${i}@example.com`, password: 'password123' } });
    for (let i = 0; i < 3; i++) expect((await r(i, '10.0.0.1')).status).toBe(201);
    const blocked = await r(3, '10.0.0.1');
    expect(blocked.status).toBe(429);
    expect(blocked.body.error.code).toBe('rate_limited');
    expect(Number(blocked.headers['retry-after'])).toBeGreaterThan(0);
    expect((await r(4, '10.0.0.2')).status).toBe(201);
    c.clock.t += 3601 * 1000;
    expect((await r(3, '10.0.0.1')).status).toBe(201);
  });
});

describe('login lockout', () => {
  let c;
  beforeAll(async () => { c = await makeCtx({ cfg: { LOGIN_MAX_FAILURES: '3', LOGIN_LOCKOUT_SEC: '600' } }); });
  afterAll(() => c.close());
  const login = (password) => c.call('POST', '/auth/login', { body: { login: 'victim', password } });

  it('locks the account after N failures (even for the right password) and unlocks later; success resets the counter', async () => {
    await c.register('victim');
    expect((await login('nope-nope-1')).status).toBe(401);
    expect((await login('nope-nope-2')).status).toBe(401);
    expect((await login('password123')).status).toBe(200); // success resets failures
    for (let i = 0; i < 3; i++) expect((await login('nope-nope')).status).toBe(401);
    const locked = await login('password123');
    expect(locked.status).toBe(429);
    expect(Number(locked.headers['retry-after'])).toBeGreaterThan(0);
    c.clock.t += 601 * 1000;
    expect((await login('password123')).status).toBe(200);
  });
});

describe('login limit per IP', () => {
  let c;
  beforeAll(async () => { c = await makeCtx({ cfg: { LOGIN_IP_MAX: '4', LOGIN_IP_WINDOW_SEC: '60' } }); });
  afterAll(() => c.close());

  it('throttles one IP across usernames', async () => {
    const statuses = [];
    for (let i = 0; i < 6; i++) statuses.push((await c.call('POST', '/auth/login', { ip: '10.9.9.9', body: { login: `u${i}`, password: 'whatever12' } })).status);
    expect(statuses).toEqual([401, 401, 401, 401, 429, 429]);
  });
});
