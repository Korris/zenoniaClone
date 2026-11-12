import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { armor, makeCtx, weapon } from './helpers.js';
import { keepWantedDay } from '../src/rpg/wanted-day.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const DAY = 20405; // a WantedBoss.DayKey
const save = (o, state, rev) => c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev, state });
const load = async (o) => (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
const redeemAgain = async (o) => {
  // Release the lock, enter again and redeem: what a relog does.
  await c.svc('POST', `/internal/sessions/${o.sessionId}/end`, { characterId: o.char.id });
  const e = await c.call('POST', `/characters/${o.char.id}/enter`, { token: o.user.accessToken });
  return (await c.svc('POST', '/internal/tickets/redeem', { ticket: e.body.ticket })).body;
};

describe('keepWantedDay', () => {
  it('keeps the larger day and tolerates missing values', () => {
    const n = { wantedDay: 3 }; keepWantedDay({ wantedDay: 9 }, n); expect(n.wantedDay).toBe(9);
    const m = { wantedDay: 12 }; keepWantedDay({ wantedDay: 9 }, m); expect(m.wantedDay).toBe(12);
    const k = {}; keepWantedDay(undefined, k); expect(k.wantedDay).toBe(0);
  });
});

describe('wantedDay persistence', () => {
  it('a new character starts at 0', async () => {
    const o = await c.online();
    expect(o.state.wantedDay).toBe(0);
  });

  it('is stored by a save, shown to the owner and returned to the zone after a relog', async () => {
    const o = await c.online();
    expect((await save(o, { ...o.state, wantedDay: DAY }, o.rev)).status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY);
    const again = await redeemAgain(o);
    expect(again.character.state.wantedDay).toBe(DAY);
  });

  it('never goes backwards: a lower value or a missing field keeps the stored day', async () => {
    const o = await c.online();
    expect((await save(o, { ...o.state, wantedDay: DAY }, 0)).status).toBe(200);
    expect((await save(o, { ...o.state, wantedDay: DAY - 5 }, 1)).status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY);
    const { wantedDay, ...oldBuild } = o.state; // a zone build that does not know the field
    expect((await save(o, oldBuild, 2)).status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY);
    expect((await save(o, { ...o.state, wantedDay: DAY + 1 }, 3)).status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY + 1);
  });

  it('rejects a day outside 0..1e6', async () => {
    const o = await c.online();
    expect((await save(o, { ...o.state, wantedDay: -1 }, 0)).status).toBe(400);
    expect((await save(o, { ...o.state, wantedDay: 1_000_001 }, 0)).status).toBe(400);
    expect((await save(o, { ...o.state, wantedDay: 1.5 }, 0)).status).toBe(400);
  });

  it('survives enhance (works on the stored state)', async () => {
    const o = await c.online();
    const r = await save(o, { ...o.state, wantedDay: DAY, upgradeStone: 3, inventory: [weapon('w1', 0)] }, 0);
    c.rng.value = 0;
    const e = await c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: o.sessionId, rev: r.body.rev, reqId: 'req-wanted-01', itemUid: 'w1' });
    expect(e.status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY);
  });

  it('survives an inventory ops batch', async () => {
    const o = await c.online();
    const r = await save(o, { ...o.state, wantedDay: DAY, inventory: [weapon('w1')] }, 0);
    const ops = await c.svc('POST', `/internal/characters/${o.char.id}/ops`, { sessionId: o.sessionId, rev: r.body.rev, reqId: 'req-wanted-ops1', ops: [{ type: 'equip', uid: 'w1' }] });
    expect(ops.status).toBe(200);
    expect((await load(o)).state.wantedDay).toBe(DAY);
  });

  it('a trade commit keeps each side\'s own day', async () => {
    const a = await c.online('warrior');
    const b = await c.online('wizard');
    const sa = { ...a.state, wantedDay: DAY, gold: 100, inventory: [armor('a-armor')] };
    const sb = { ...b.state, wantedDay: DAY + 2, gold: 100 };
    await save(a, sa, 0); await save(b, sb, 0);
    const side = (o, state, giveUids = []) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: 1, state, giveUids, giveGold: 0 });
    const r = await c.svc('POST', '/internal/trades/commit', { tradeId: `trade-wanted-${Date.now()}`, a: side(a, sa, ['a-armor']), b: side(b, sb) });
    expect(r.status).toBe(200);
    expect((await load(a)).state.wantedDay).toBe(DAY);
    expect((await load(b)).state.wantedDay).toBe(DAY + 2);
  });
});
