import net from 'node:net';
import { Writable } from 'node:stream';
import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { loadConfig } from '../src/config.js';
import { createMailer, smtpMissing } from '../src/mail/mailer.js';
import { loggerOptions } from '../src/logging.js';
import { buildApp } from '../src/app.js';
import { makeCtx, testConfig } from './helpers.js';

const base = { MONGO_URI: 'x', JWT_SECRET: 'a'.repeat(40), SERVICE_KEY: 'b'.repeat(20) };
const smtp = { SMTP_HOST: 'smtp.gmail.com', SMTP_USER: 'me@gmail.com', SMTP_PASS: 'app-password', SMTP_FROM: 'me@gmail.com' };

function fakeLog() {
  const lines = [];
  const add = (level) => (a, b) => lines.push({ level, text: JSON.stringify([a, b]) });
  return { lines, info: add('info'), warn: add('warn'), error: add('error') };
}

/** Just enough of an SMTP server over a real socket, so the real nodemailer transport is what runs. */
function fakeSmtpServer() {
  const mails = [];
  const server = net.createServer((sock) => {
    let inData = false, data = '', buf = '';
    sock.write('220 fake ESMTP\r\n');
    sock.on('error', () => {});
    sock.on('data', (chunk) => {
      buf += chunk.toString('latin1');
      let i;
      while ((i = buf.indexOf('\r\n')) >= 0) {
        const line = buf.slice(0, i);
        buf = buf.slice(i + 2);
        if (inData) {
          if (line === '.') { inData = false; mails.push(data); sock.write('250 queued\r\n'); } else data += `${line}\n`;
        } else if (/^EHLO/i.test(line)) sock.write('250-fake\r\n250 AUTH PLAIN LOGIN\r\n');
        else if (/^AUTH/i.test(line)) sock.write('235 ok\r\n');
        else if (/^DATA/i.test(line)) { inData = true; data = ''; sock.write('354 go\r\n'); }
        else if (/^QUIT/i.test(line)) { sock.write('221 bye\r\n'); sock.end(); }
        else sock.write('250 ok\r\n');
      }
    });
  });
  return new Promise((resolve) => server.listen(0, '127.0.0.1', () => resolve({ mails, port: server.address().port, close: () => server.close() })));
}

const sent = { email: 'a@example.com', username: 'alice', link: 'https://x.test/reset#token=SECRETTOKEN', token: 'SECRETTOKEN' };

describe('createMailer', () => {
  it('in production without SMTP warns at startup, says so per request, and never logs the token, link or address', async () => {
    const log = fakeLog();
    const m = createMailer({ cfg: loadConfig({ ...base, NODE_ENV: 'production', SMTP_HOST: 'smtp.gmail.com' }), log });
    expect(m.enabled).toBe(false);
    expect(log.lines.some((l) => l.level === 'warn' && /NOT be sent/.test(l.text) && /SMTP_PASS/.test(l.text))).toBe(true);
    await m.sendPasswordReset(sent);
    expect(log.lines.some((l) => /no mail was sent/.test(l.text))).toBe(true);
    const all = log.lines.map((l) => l.text).join('\n');
    expect(all).not.toContain('SECRETTOKEN');
    expect(all).not.toContain('a@example.com');
  });

  it('outside production without SMTP logs the link', async () => {
    const log = fakeLog();
    const m = createMailer({ cfg: loadConfig(base), log });
    await m.sendPasswordReset(sent);
    expect(log.lines.map((l) => l.text).join()).toContain('SECRETTOKEN');
  });

  it('every one of the Gmail fields is needed before SMTP counts as on', () => {
    for (const k of Object.keys(smtp)) expect(smtpMissing(loadConfig({ ...base, ...smtp, [k]: '' }))).toEqual([k]);
    expect(smtpMissing(loadConfig({ ...base, ...smtp }))).toEqual([]);
  });

  it('Gmail settings mean STARTTLS on 587 with encryption required, not implicit TLS', () => {
    let options;
    const m = createMailer({ cfg: loadConfig({ ...base, ...smtp, NODE_ENV: 'production', PASSWORD_RESET_URL: 'https://x.test/reset' }), log: fakeLog(), createTransport: (o) => { options = o; return {}; } });
    expect(m.enabled).toBe(true);
    expect(options).toMatchObject({ host: 'smtp.gmail.com', port: 587, secure: false, requireTLS: true, auth: { user: 'me@gmail.com', pass: 'app-password' } });
  });

  it('sends a real SMTP mail holding the link, and logs neither the link nor the password', async () => {
    const server = await fakeSmtpServer();
    const log = fakeLog();
    const m = createMailer({
      cfg: loadConfig({ ...base, ...smtp, SMTP_HOST: '127.0.0.1', SMTP_PORT: String(server.port), SMTP_REQUIRE_TLS: 'false' }), log,
    });
    await m.sendPasswordReset(sent);
    server.close();
    expect(server.mails).toHaveLength(1);
    const raw = server.mails[0].replace(/=\n/g, '');
    expect(raw).toContain('To: a@example.com');
    expect(raw).toContain('From: me@gmail.com');
    expect(raw).toContain('SECRETTOKEN');
    const all = log.lines.map((l) => l.text).join('\n');
    expect(all).not.toContain('SECRETTOKEN');
    expect(all).not.toContain('app-password');
  });
});

describe('config in production', () => {
  it('refuses the placeholder secrets of the example files', () => {
    expect(() => loadConfig({ ...base, NODE_ENV: 'production', JWT_SECRET: 'change-me-change-me-change-me-change-me' })).toThrow(/JWT_SECRET/);
    expect(() => loadConfig({ ...base, NODE_ENV: 'production', SERVICE_KEY: 'dev-only-service-key' })).toThrow(/SERVICE_KEY/);
    expect(loadConfig({ ...base, NODE_ENV: 'production' }).NODE_ENV).toBe('production');
    expect(loadConfig({ ...base, SERVICE_KEY: 'dev-only-service-key' }).SERVICE_KEY).toBe('dev-only-service-key');
  });
});

describe('reset flow over HTTP', () => {
  let c;
  beforeAll(async () => { c = await makeCtx(); await c.register('resetuser'); });
  afterAll(async () => { await c.close(); });
  const request = () => c.call('POST', '/auth/password-reset/request', { body: { email: 'resetuser@example.com' } });

  it('puts the token in a fragment, and sends one mail per cooldown', async () => {
    await request();
    expect(c.resets).toHaveLength(1);
    expect(c.resets[0].link).toMatch(/\/reset#token=[A-Za-z0-9_-]+$/);
    expect((await request()).status).toBe(202);
    expect(c.resets).toHaveLength(1);
    c.clock.t += 61_000;
    await request();
    expect(c.resets).toHaveLength(2);
  });

  it('an older mail stops working once the password has been changed', async () => {
    const [older, newer] = c.resets;
    expect((await c.call('POST', '/auth/password-reset/confirm', { body: { token: newer.token, password: 'brand-new-pass1' } })).status).toBe(200);
    expect((await c.call('POST', '/auth/password-reset/confirm', { body: { token: older.token, password: 'other-new-pass2' } })).status).toBe(401);
  });

  it('serves the page with no way for a token to leak', async () => {
    const r = await c.app.inject({ method: 'GET', url: '/reset' });
    expect(r.statusCode).toBe(200);
    expect(r.headers['content-type']).toContain('text/html');
    expect(r.headers['referrer-policy']).toBe('no-referrer');
    expect(r.headers['cache-control']).toBe('no-store');
    expect(r.headers['content-security-policy']).toContain("default-src 'none'");
    expect(r.headers['content-security-policy']).toContain("connect-src 'self'");
    expect(r.body).toContain('/auth/password-reset/confirm');
    expect(r.body).toContain('history.replaceState');
  });

  it('sends no CORS header, and /health says nothing but ok', async () => {
    const r = await c.app.inject({ method: 'GET', url: '/health', headers: { origin: 'https://evil.example' } });
    expect(r.headers['access-control-allow-origin']).toBeUndefined();
    expect(JSON.parse(r.body)).toEqual({ ok: true });
  });
});

describe('what reaches the log', () => {
  it('a query string does not, nor do passwords and tokens in bodies', async () => {
    let out = '';
    const stream = new Writable({ write(chunk, _e, cb) { out += chunk; cb(); } });
    const c = await makeCtx();
    const app = buildApp({ db: c.db, cfg: testConfig(), mailer: { sendPasswordReset: async () => {} }, logger: { ...loggerOptions(), stream } });
    await app.ready();
    await app.inject({ method: 'GET', url: '/reset?token=QUERYSECRET' });
    await app.inject({ method: 'POST', url: '/auth/password-reset/confirm', payload: { token: 'BODYSECRET-BODYSECRET-1', password: 'BODYPASSWORD1' } });
    await app.close();
    await c.close();
    expect(out).toContain('/reset');
    expect(out).toContain('incoming request');
    expect(out).not.toContain('QUERYSECRET');
    expect(out).not.toContain('BODYSECRET');
    expect(out).not.toContain('BODYPASSWORD');
  });

  it('a failing mail server gives the same 202 and logs no token', async () => {
    let out = '';
    const stream = new Writable({ write(chunk, _e, cb) { out += chunk; cb(); } });
    const c = await makeCtx();
    await c.register('failmail');
    let token;
    const app = buildApp({ db: c.db, cfg: testConfig(), logger: { ...loggerOptions(), stream }, mailer: { sendPasswordReset: async (m) => { token = m.token; throw Object.assign(new Error('boom'), { code: 'EAUTH', responseCode: 535 }); } } });
    await app.ready();
    const r = await app.inject({ method: 'POST', url: '/auth/password-reset/request', payload: { email: 'failmail@example.com' } });
    await new Promise((res) => setTimeout(res, 20));
    await app.close();
    await c.close();
    expect(r.statusCode).toBe(202);
    expect(token).toBeTruthy();
    expect(out).toContain('password reset mail could not be sent');
    expect(out).toContain('EAUTH');
    expect(out).not.toContain(token);
  });
});
