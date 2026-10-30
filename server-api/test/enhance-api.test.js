import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx, weapon } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

/** Online warrior with a +0 weapon in the bag and `stones` Upgrade Stones. rev is tracked by the caller. */
async function geared(stones = 5, enhance = 0) {
  const o = await c.online();
  const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, {
    sessionId: o.sessionId, rev: 0,
    state: { ...o.state, upgradeStone: stones, inventory: [weapon('w1', 0)], equipment: [] },
  });
  o.rev = r.body.rev;
  if (enhance) { // the API itself raises levels: grant via enhance with a roll of 0 until the target
    for (let i = 0; i < enhance; i++) { c.rng.value = 0; const e = await enhance_(o, 'w1', `seed-${i}-${o.char.id}`); o.rev = e.body.rev; }
  }
  return o;
}
const enhance_ = (o, itemUid, reqId) => c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: o.sessionId, rev: o.rev, reqId, itemUid });

describe('POST /internal/characters/:id/enhance', () => {
  it('success: +1 level, -1 stone, rev bumped, persisted', async () => {
    const o = await geared(5);
    c.rng.value = 0.0;
    const r = await enhance_(o, 'w1', 'req-ok-0001');
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ ok: true, success: true, level: 1, upgradeStone: 4, rev: o.rev + 1, replayed: false });
    const stored = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(stored.state.inventory[0].enhance).toBe(1);
    expect(stored.state.upgradeStone).toBe(4);
  });

  it('failure still costs the stone and keeps the level', async () => {
    const o = await geared(5, 3); // +3 reached with roll 0 (guaranteed anyway); next target +4 = 90 %
    c.rng.value = 0.95;
    const r = await enhance_(o, 'w1', 'req-fail-001');
    expect(r.body).toMatchObject({ ok: true, success: false, level: 3, upgradeStone: 1 });
  });

  it('boundary: roll exactly equal to the chance fails, just below succeeds', async () => {
    const o = await geared(5, 3);
    c.rng.value = 0.9;
    const fail = await enhance_(o, 'w1', 'req-edge-001');
    expect(fail.body.success).toBe(false);
    o.rev = fail.body.rev;
    c.rng.value = 0.9 - 1e-9;
    expect((await enhance_(o, 'w1', 'req-edge-002')).body).toMatchObject({ success: true, level: 4 });
  });

  it('replaying the same reqId never double-applies (even with a lost response / old rev)', async () => {
    const o = await geared(5);
    c.rng.value = 0;
    const first = await enhance_(o, 'w1', 'req-dupe-001');
    c.rng.value = 0.99; // a re-roll would now fail; the replay must return the original outcome
    const replay = await enhance_(o, 'w1', 'req-dupe-001');
    expect(replay.status).toBe(200);
    expect(replay.body).toMatchObject({ replayed: true, success: true, level: 1, upgradeStone: 4, rev: first.body.rev });
    const stored = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(stored.state.upgradeStone).toBe(4);
    expect(stored.rev).toBe(first.body.rev);
    expect(await c.db.collection('ledger').countDocuments({ characterId: o.char.id, reqId: 'req-dupe-001' })).toBe(1);
  });

  it('parallel duplicate requests apply once', async () => {
    const o = await geared(5);
    c.rng.value = 0;
    const rs = await Promise.all([1, 2, 3].map(() => enhance_(o, 'w1', 'req-race-001')));
    expect(rs.filter((r) => r.status === 200 && r.body.replayed === false)).toHaveLength(1);
    const stored = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(stored.state.upgradeStone).toBe(4);
  });

  it('refusals return ok:false with the C# reason and change nothing (no rev bump)', async () => {
    const o = await geared(0);
    const noStone = await enhance_(o, 'w1', 'req-nost-001');
    expect(noStone.body).toMatchObject({ ok: false, reason: 'no-cost', rev: o.rev });
    expect((await enhance_(o, 'ghost', 'req-ghost-01')).body.reason).toBe('not-owned');
  });

  it('stops at +15 (max-level)', async () => {
    const o = await geared(30, 15);
    expect(o.rev).toBeGreaterThan(0);
    const r = await enhance_(o, 'w1', 'req-max-0001');
    expect(r.body).toMatchObject({ ok: false, reason: 'max-level', level: 15 });
  });

  it('rejects stale rev, foreign session, bad input, missing service key', async () => {
    const o = await geared(5);
    const stale = await c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: o.sessionId, rev: o.rev - 1, reqId: 'req-stale-01', itemUid: 'w1' });
    expect(stale.body.error.code).toBe('rev_conflict');
    const wrongSession = await c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: 'b'.repeat(32), rev: o.rev, reqId: 'req-sess-001', itemUid: 'w1' });
    expect(wrongSession.body.error.code).toBe('session_mismatch');
    expect((await c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: o.sessionId, rev: o.rev, reqId: 'short', itemUid: 'w1' })).status).toBe(400);
    expect((await c.call('POST', `/internal/characters/${o.char.id}/enhance`, { body: {} })).status).toBe(401);
  });

  it('an enhanced level survives a later zone save of the same item (no regression, no tamper)', async () => {
    const o = await geared(5);
    c.rng.value = 0;
    const e = await enhance_(o, 'w1', 'req-keep-001');
    const state = { ...e.body.state, gold: 9 };
    const s = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: e.body.rev, state });
    expect(s.status).toBe(200);
  });
});
