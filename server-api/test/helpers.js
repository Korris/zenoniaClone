import crypto from 'node:crypto';
import { inject } from 'vitest';
import { MongoClient } from 'mongodb';
import { buildApp } from '../src/app.js';
import { loadConfig } from '../src/config.js';
import { ensureIndexes } from '../src/db/indexes.js';

export const SERVICE_KEY = 'test-service-key-0123456789';

export function testConfig(overrides = {}) {
  return loadConfig({
    MONGO_URI: 'unused', JWT_SECRET: 'x'.repeat(40), SERVICE_KEY, BCRYPT_COST: '4',
    LOGIN_IP_MAX: '1000', REGISTER_IP_MAX: '1000', ...overrides,
  });
}

/** Fresh database + app per call. `clock.t` is a mutable fake time; `rng.value` is the injected enhance roll. */
export async function makeCtx({ cfg: cfgOverrides } = {}) {
  const client = await new MongoClient(inject('mongoUri')).connect();
  const db = client.db(`t_${crypto.randomBytes(6).toString('hex')}`);
  await ensureIndexes(db);
  const clock = { t: Date.now() };
  const rng = { value: 0 };
  const resets = [];
  const app = buildApp({
    db, cfg: testConfig(cfgOverrides), now: () => clock.t, rand: () => rng.value,
    mailer: { sendPasswordReset: async (m) => { resets.push(m); } },
  });
  await app.ready();

  const call = async (method, url, { body, token, service, ip } = {}) => {
    const headers = {};
    if (token) headers.authorization = `Bearer ${token}`;
    if (service) headers['x-service-key'] = service === true ? SERVICE_KEY : service;
    const r = await app.inject({ method, url, payload: body, headers, remoteAddress: ip });
    return { status: r.statusCode, body: r.body ? JSON.parse(r.body) : null, headers: r.headers };
  };
  const svc = (method, url, body) => call(method, url, { body, service: true });

  let n = 0;
  const register = async (name = `user${++n}`) => {
    const r = await call('POST', '/auth/register', { body: { username: name, email: `${name}@example.com`, password: 'password123' } });
    return { ...r.body, res: r };
  };
  const createChar = async (token, name, classId = 'warrior') => (await call('POST', '/characters', { token, body: { name, classId } })).body.character;
  /** register + character + enter + redeem => ready-to-save session. */
  const online = async (classId = 'warrior') => {
    const u = await register();
    const c = await createChar(u.accessToken, `hero${++n}`, classId);
    const e = await call('POST', `/characters/${c.id}/enter`, { token: u.accessToken });
    const r = await svc('POST', '/internal/tickets/redeem', { ticket: e.body.ticket });
    return { user: u, char: c, ticket: e.body.ticket, sessionId: r.body.sessionId, rev: r.body.character.rev, state: r.body.character.state };
  };
  const close = async () => { await app.close(); await db.dropDatabase(); await client.close(); };
  return { app, db, call, svc, clock, rng, resets, register, createChar, online, close };
}

export const weapon = (uid, enhance = 0) => ({
  uid, templateId: 'sword-1', slot: 'weapon', name: 'Test Sword', rarity: 'common', classId: 'warrior', itemLevel: 1, enhance,
  stats: { atk: 10, def: 0, maxHp: 0, maxMp: 0, critChance: 0, speedMult: 0 },
});
export const armor = (uid) => ({
  uid, templateId: 'armor-1', slot: 'armor', name: 'Test Armor', rarity: 'rare', classId: '', itemLevel: 3, enhance: 0,
  stats: { atk: 0, def: 5, maxHp: 0, maxMp: 0, critChance: 0, speedMult: 0 },
});
