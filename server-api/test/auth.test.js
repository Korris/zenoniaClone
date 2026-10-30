import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const reg = (body, ip) => c.call('POST', '/auth/register', { body, ip });
const good = { username: 'Alice_1', email: 'Alice@Example.com', password: 'hunter2hunter2' };
const login = (login, password) => c.call('POST', '/auth/login', { body: { login, password } });

describe('register', () => {
  it('creates an account and returns tokens, hashing the password', async () => {
    const r = await reg(good);
    expect(r.status).toBe(201);
    expect(r.body.user).toMatchObject({ username: 'Alice_1', email: 'alice@example.com' });
    expect(r.body.accessToken).toBeTruthy();
    expect(r.body.refreshToken).toBeTruthy();
    const doc = await c.db.collection('users').findOne({ usernameLower: 'alice_1' });
    expect(doc.passHash).toMatch(/^\$2[aby]\$/);
    expect(JSON.stringify(r.body)).not.toContain(doc.passHash);
  });

  it('rejects duplicate username (any case) and duplicate email with 409', async () => {
    expect((await reg({ ...good, email: 'other@example.com', username: 'ALICE_1' })).body.error.code).toBe('username_taken');
    const r = await reg({ ...good, username: 'Bob' });
    expect(r.status).toBe(409);
    expect(r.body.error.code).toBe('email_taken');
  });

  it.each([
    [{ ...good, username: 'ab' }],
    [{ ...good, username: 'bad name!' }],
    [{ ...good, email: 'not-an-email' }],
    [{ ...good, password: 'short' }],
    [{ ...good, password: 'x'.repeat(73) }],
    [{ username: 'only' }],
  ])('rejects invalid input %#', async (body) => {
    const r = await reg(body);
    expect(r.status).toBe(400);
    expect(r.body.error.code).toBe('validation_error');
    expect(Array.isArray(r.body.error.details)).toBe(true);
  });
});

describe('login / refresh / logout', () => {
  it('logs in by username or email; wrong password and unknown user give the same 401', async () => {
    expect((await login('alice_1', good.password)).status).toBe(200);
    expect((await login('ALICE@example.com', good.password)).status).toBe(200);
    const bad = await login('alice_1', 'wrong-password');
    const none = await login('nobody', 'wrong-password');
    expect(bad.status).toBe(401);
    expect(none.body).toEqual(bad.body); // no account enumeration
  });

  it('access token authorizes /characters; garbage / missing token is 401', async () => {
    const { body } = await login('alice_1', good.password);
    expect((await c.call('GET', '/characters', { token: body.accessToken })).status).toBe(200);
    expect((await c.call('GET', '/characters', { token: 'garbage' })).body.error.code).toBe('invalid_token');
    expect((await c.call('GET', '/characters')).status).toBe(401);
  });

  it('refresh rotates the token; reusing the old one revokes the whole family', async () => {
    const { body: a } = await login('alice_1', good.password);
    const r1 = await c.call('POST', '/auth/refresh', { body: { refreshToken: a.refreshToken } });
    expect(r1.status).toBe(200);
    expect(r1.body.refreshToken).not.toBe(a.refreshToken);
    expect((await c.call('GET', '/characters', { token: r1.body.accessToken })).status).toBe(200);
    // replay of the rotated-out token => theft signal => the new token dies too
    expect((await c.call('POST', '/auth/refresh', { body: { refreshToken: a.refreshToken } })).status).toBe(401);
    expect((await c.call('POST', '/auth/refresh', { body: { refreshToken: r1.body.refreshToken } })).status).toBe(401);
  });

  it('logout revokes the refresh token', async () => {
    const { body: a } = await login('alice_1', good.password);
    expect((await c.call('POST', '/auth/logout', { body: { refreshToken: a.refreshToken } })).status).toBe(200);
    expect((await c.call('POST', '/auth/refresh', { body: { refreshToken: a.refreshToken } })).status).toBe(401);
  });

  it('refresh fails after the refresh token expires', async () => {
    const { body: a } = await login('alice_1', good.password);
    c.clock.t += 31 * 24 * 3600 * 1000;
    expect((await c.call('POST', '/auth/refresh', { body: { refreshToken: a.refreshToken } })).status).toBe(401);
    c.clock.t -= 31 * 24 * 3600 * 1000;
  });
});

describe('password reset stub', () => {
  it('logs a link instead of mailing, answers 202 for unknown emails too, and the token works once', async () => {
    const unknown = await c.call('POST', '/auth/password-reset/request', { body: { email: 'ghost@example.com' } });
    expect(unknown.status).toBe(202);
    expect(c.resets).toHaveLength(0);
    expect((await c.call('POST', '/auth/password-reset/request', { body: { email: good.email } })).status).toBe(202);
    expect(c.resets).toHaveLength(1);
    const { token, link } = c.resets[0];
    expect(link).toContain(token);

    const { body: a } = await login('alice_1', good.password);
    const done = await c.call('POST', '/auth/password-reset/confirm', { body: { token, password: 'brand-new-pass1' } });
    expect(done.status).toBe(200);
    expect((await c.call('POST', '/auth/password-reset/confirm', { body: { token, password: 'another-pass-22' } })).status).toBe(401);
    expect((await c.call('POST', '/auth/refresh', { body: { refreshToken: a.refreshToken } })).status).toBe(401); // devices signed out
    expect((await login('alice_1', good.password)).status).toBe(401);
    expect((await login('alice_1', 'brand-new-pass1')).status).toBe(200);
  });
});

describe('error format', () => {
  it('unknown route and malformed JSON use the same envelope', async () => {
    const nf = await c.call('GET', '/nope');
    expect(nf.status).toBe(404);
    expect(nf.body.error.code).toBe('not_found');
    const r = await c.app.inject({ method: 'POST', url: '/auth/login', payload: '{bad', headers: { 'content-type': 'application/json' } });
    expect(r.statusCode).toBe(400);
    expect(JSON.parse(r.body).error.code).toBe('bad_request');
  });
});
