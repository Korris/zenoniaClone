import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

async function setup() {
  const u = await c.register();
  const ch = await c.createChar(u.accessToken, `Hero${Math.random().toString(36).slice(2, 8)}`);
  const enter = () => c.call('POST', `/characters/${ch.id}/enter`, { token: u.accessToken });
  return { u, ch, enter };
}
const redeem = (ticket) => c.svc('POST', '/internal/tickets/redeem', { ticket });

describe('zone tickets', () => {
  it('enter returns zone address + ticket; redeem returns character + sessionId', async () => {
    const { ch, enter } = await setup();
    const e = await enter();
    expect(e.status).toBe(200);
    expect(e.body).toMatchObject({ zoneId: 'frost-hollow', zoneHost: '127.0.0.1', zonePort: 9050, expiresIn: 60 });
    const r = await redeem(e.body.ticket);
    expect(r.status).toBe(200);
    expect(r.body.sessionId).toMatch(/^[a-f0-9]{32}$/);
    expect(r.body.character).toMatchObject({ id: ch.id, name: ch.name, rev: 0 });
    expect(r.body.character.state.classId).toBe('warrior');
  });

  it('a ticket is single use', async () => {
    const { enter } = await setup();
    const { body } = await enter();
    expect((await redeem(body.ticket)).status).toBe(200);
    const again = await redeem(body.ticket);
    expect(again.status).toBe(401);
    expect(again.body.error.code).toBe('invalid_ticket');
  });

  it('two concurrent redeems of one ticket: exactly one wins', async () => {
    const { enter } = await setup();
    const { body } = await enter();
    const results = await Promise.all([redeem(body.ticket), redeem(body.ticket), redeem(body.ticket)]);
    expect(results.filter((r) => r.status === 200)).toHaveLength(1);
  });

  it('a ticket expires after 60 s', async () => {
    const { enter } = await setup();
    const { body } = await enter();
    c.clock.t += 61_000;
    expect((await redeem(body.ticket)).body.error.code).toBe('invalid_ticket');
  });

  it('a new enter supersedes the previous unused ticket', async () => {
    const { enter } = await setup();
    const first = (await enter()).body.ticket;
    const second = (await enter()).body.ticket;
    expect((await redeem(first)).status).toBe(401);
    expect((await redeem(second)).status).toBe(200);
  });

  it('unknown tickets and other users cannot enter; enter needs auth and ownership', async () => {
    const { ch } = await setup();
    const other = await c.register();
    expect((await redeem('x'.repeat(43))).status).toBe(401);
    expect((await c.call('POST', `/characters/${ch.id}/enter`, { token: other.accessToken })).status).toBe(404);
    expect((await c.call('POST', `/characters/${ch.id}/enter`)).status).toBe(401);
  });
});

describe('service key', () => {
  it('internal routes reject missing or wrong keys', async () => {
    expect((await c.call('POST', '/internal/tickets/redeem', { body: { ticket: 'x'.repeat(30) } })).status).toBe(401);
    const bad = await c.call('POST', '/internal/tickets/redeem', { body: { ticket: 'x'.repeat(30) }, service: 'wrong-key' });
    expect(bad.status).toBe(403);
    expect((await c.call('GET', '/internal/zones', { service: true })).body.zones[0].id).toBe('frost-hollow');
  });
  it('a user JWT is not a service credential', async () => {
    const u = await c.register();
    expect((await c.call('GET', '/internal/zones', { token: u.accessToken })).status).toBe(401);
  });
});

describe('one session per character', () => {
  it('second enter is rejected while online; allowed after the session ends', async () => {
    const { ch, enter } = await setup();
    const r = await redeem((await enter()).body.ticket);
    const blocked = await enter();
    expect(blocked.status).toBe(409);
    expect(blocked.body.error.code).toBe('already_online');
    const end = await c.svc('POST', `/internal/sessions/${r.body.sessionId}/end`, { characterId: ch.id });
    expect(end.body.released).toBe(true);
    expect((await enter()).status).toBe(200);
  });

  it('redeem is refused with already_online if a ticket was issued before the first login completed', async () => {
    const { u, ch, enter } = await setup();
    const t1 = (await enter()).body.ticket;
    // simulate a second ticket minted by a race (bypassing the supersede step)
    const t2 = await (async () => { const { createTicketService } = await import('../src/auth/ticket-service.js'); return createTicketService({ db: c.db, ttlSec: 60, now: () => c.clock.t }).create(ch.id, String(u.user.id), 'frost-hollow'); })();
    expect((await redeem(t1)).status).toBe(200);
    const second = await redeem(t2);
    expect(second.status).toBe(409);
    expect(second.body.error.code).toBe('already_online');
  });

  it('a crashed zone frees the lock after 90 s without a save heartbeat; the old session is then locked out', async () => {
    const { ch, enter } = await setup();
    const first = await redeem((await enter()).body.ticket);
    c.clock.t += 89_000;
    expect((await enter()).status).toBe(409);
    c.clock.t += 2_000; // 91 s
    const e2 = await enter();
    expect(e2.status).toBe(200);
    const second = await redeem(e2.body.ticket);
    expect(second.body.sessionId).not.toBe(first.body.sessionId);
    const stale = await c.svc('POST', `/internal/characters/${ch.id}/save`, { sessionId: first.body.sessionId, rev: 0, state: first.body.character.state });
    expect(stale.status).toBe(409);
    expect(stale.body.error.code).toBe('session_mismatch');
  });

  it('end of a stale session does not release the new lock', async () => {
    const { ch, enter } = await setup();
    const first = await redeem((await enter()).body.ticket);
    c.clock.t += 91_000;
    await redeem((await enter()).body.ticket);
    const end = await c.svc('POST', `/internal/sessions/${first.body.sessionId}/end`, { characterId: ch.id });
    expect(end.body.released).toBe(false);
    expect((await enter()).status).toBe(409);
  });
});
