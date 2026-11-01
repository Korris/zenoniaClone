import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { armor, makeCtx, weapon } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

let nextId = 0;
const newTradeId = () => `trade-${Date.now()}-${++nextId}`;
const side = (o, state, giveUids = [], giveGold = 0, rev = o.rev, sessionId = o.sessionId) => ({ characterId: o.char.id, sessionId, rev, state, giveUids, giveGold });
const commit = (a, b, tradeId = newTradeId()) => c.svc('POST', '/internal/trades/commit', { tradeId, a, b });
const load = async (o) => (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
const goods = (char) => ({ gold: char.state.gold, bag: char.state.inventory.map((i) => i.uid).sort() });

/** Two online characters, A holds armor + a weapon + 1000 G (stored), B holds an item + 500 G. */
async function pair() {
  const a = await c.online('warrior');
  const b = await c.online('wizard');
  const stateA = { ...a.state, gold: 1000, upgradeStone: 5, inventory: [armor('a-armor'), weapon('a-sword')] };
  const stateB = { ...b.state, gold: 500, inventory: [armor('b-armor')] };
  await c.svc('POST', `/internal/characters/${a.char.id}/save`, { sessionId: a.sessionId, rev: 0, state: stateA });
  await c.svc('POST', `/internal/characters/${b.char.id}/save`, { sessionId: b.sessionId, rev: 0, state: stateB });
  return { a: { ...a, rev: 1, state: stateA }, b: { ...b, rev: 1, state: stateB } };
}

describe('trade commit', () => {
  it('moves items and gold both ways, bumps both revs, writes the ledger', async () => {
    const { a, b } = await pair();
    const r = await commit(side(a, a.state, ['a-armor'], 300), side(b, b.state, ['b-armor'], 100));
    expect(r.status).toBe(200);
    expect(r.body).toEqual({ replayed: false, a: { rev: 2 }, b: { rev: 2 } });
    expect(goods(await load(a))).toEqual({ gold: 800, bag: ['a-sword', 'b-armor'] });
    expect(goods(await load(b))).toEqual({ gold: 700, bag: ['a-armor'] });
    const entry = await c.db.collection('ledger').findOne({ characterId: a.char.id, type: 'trade' });
    expect(entry.after.a.gave.map((i) => i.uid)).toEqual(['a-armor']);
    expect(entry.after.b.goldGiven).toBe(100);
  });

  it('includes what the zone holds beyond the last save (a pickup since then is kept)', async () => {
    const { a, b } = await pair();
    const newer = { ...a.state, gold: 1200, inventory: [...a.state.inventory, armor('a-picked')] };
    expect((await commit(side(a, newer, ['a-picked'], 50), side(b, b.state))).status).toBe(200);
    expect(goods(await load(a))).toEqual({ gold: 1150, bag: ['a-armor', 'a-sword'] });
    expect(goods(await load(b))).toEqual({ gold: 550, bag: ['a-picked', 'b-armor'] });
  });

  it('a retry of the same tradeId never applies twice', async () => {
    const { a, b } = await pair();
    const id = newTradeId();
    const first = await commit(side(a, a.state, ['a-armor'], 300), side(b, b.state), id);
    const again = await commit(side(a, a.state, ['a-armor'], 300), side(b, b.state), id); // the answer was lost, same body
    expect(first.body.replayed).toBe(false);
    expect(again.status).toBe(200);
    expect(again.body).toEqual({ replayed: true, a: { rev: 2 }, b: { rev: 2 } });
    expect(goods(await load(a))).toEqual({ gold: 700, bag: ['a-sword'] });
    expect(goods(await load(b))).toEqual({ gold: 800, bag: ['a-armor', 'b-armor'] });
  });

  it('is all or nothing: one bad side leaves both characters untouched', async () => {
    const { a, b } = await pair();
    const code = async (sa, sb) => (await commit(sa, sb)).body.error?.code;
    expect(await code(side(a, a.state, ['ghost']), side(b, b.state))).toBe('trade_item_missing');
    expect(await code(side(a, a.state, [], 1001), side(b, b.state))).toBe('insufficient_funds');
    expect(await code(side(a, a.state, ['a-armor', 'a-armor']), side(b, b.state))).toBe('trade_duplicate_item');
    expect(await code(side(a, a.state), side(b, b.state))).toBe('trade_empty');
    expect(await code(side(a, a.state, ['a-armor'], 0, 0), side(b, b.state))).toBe('rev_conflict'); // stale on A
    expect(await code(side(a, a.state, ['a-armor']), side(b, b.state, [], 0, 0))).toBe('rev_conflict'); // stale on B: A must not have moved
    expect(await code(side(a, a.state, ['a-armor']), side(b, b.state, [], 0, 1, 'a'.repeat(32)))).toBe('session_mismatch');
    const full = { ...b.state, inventory: Array.from({ length: 20 }, (_, i) => armor(`f-${i}`)) };
    expect(await code(side(a, a.state, ['a-armor']), side(b, full))).toBe('trade_invalid'); // no room
    const clash = { ...b.state, inventory: [armor('a-armor')] };
    expect(await code(side(a, a.state, ['a-armor']), side(b, clash))).toBe('trade_invalid'); // uid already held
    expect((await load(a)).rev).toBe(1);
    expect((await load(b)).rev).toBe(1);
    expect(goods(await load(a))).toEqual({ gold: 1000, bag: ['a-armor', 'a-sword'] });
    expect(goods(await load(b))).toEqual({ gold: 500, bag: ['b-armor'] });
  });

  it('refuses starter gear, tampered enhance levels and a trade with oneself', async () => {
    const { a, b } = await pair();
    const starter = { ...a.state, inventory: [armor('starter-weapon')] };
    expect((await commit(side(a, starter, ['starter-weapon']), side(b, b.state))).body.error.code).toBe('trade_starter');
    const raised = { ...a.state, inventory: [weapon('a-sword', 9)] };
    expect((await commit(side(a, raised, ['a-sword']), side(b, b.state))).body.error.code).toBe('enhance_tamper');
    expect((await commit(side(a, a.state, ['a-armor']), side(a, a.state))).body.error.code).toBe('trade_self');
  });

  it('an enhanced item keeps its level, and its new owner can save it', async () => {
    const { a, b } = await pair();
    const e = await c.svc('POST', `/internal/characters/${a.char.id}/enhance`, { sessionId: a.sessionId, rev: 1, reqId: 'enh-keep-1', itemUid: 'a-sword' });
    expect(e.body.level).toBe(1);
    expect((await commit(side(a, e.body.state, ['a-sword'], 0, e.body.rev), side(b, b.state))).status).toBe(200);
    const after = await load(b);
    expect(after.state.inventory.find((i) => i.uid === 'a-sword').enhance).toBe(1);
    expect((await c.svc('POST', `/internal/characters/${b.char.id}/save`, { sessionId: b.sessionId, rev: after.rev, state: after.state })).status).toBe(200);
  });

  it('needs the service key and validates the body', async () => {
    const { a, b } = await pair();
    const body = { tradeId: newTradeId(), a: side(a, a.state, ['a-armor']), b: side(b, b.state) };
    expect((await c.call('POST', '/internal/trades/commit', { body })).status).toBe(401);
    expect((await c.call('POST', '/internal/trades/commit', { body, service: 'wrong-key-wrong-key' })).status).toBe(403);
    expect((await c.svc('POST', '/internal/trades/commit', { ...body, tradeId: 'x' })).status).toBe(400);
    expect((await c.svc('POST', '/internal/trades/commit', { ...body, a: { ...body.a, giveGold: -1 } })).status).toBe(400);
  });

  it('races: of two commits for the same goods, exactly one wins', async () => {
    const { a, b } = await pair();
    const rs = await Promise.all([
      commit(side(a, a.state, ['a-armor']), side(b, b.state)),
      commit(side(a, a.state, ['a-armor']), side(b, b.state)),
    ]);
    expect(rs.filter((r) => r.status === 200).length).toBe(1);
    const bagA = goods(await load(a)).bag;
    const bagB = goods(await load(b)).bag;
    expect(bagA.includes('a-armor')).toBe(false);
    expect(bagB.filter((u) => u === 'a-armor').length).toBe(1);
  });
});

describe('zone restart', () => {
  it('frees the locks of its own zone only, and old sessions can never write again', async () => {
    const a = await c.online();
    expect((await c.svc('POST', '/internal/zones/other-zone/reset-sessions', {})).status).toBe(404);
    const r = await c.svc('POST', '/internal/zones/frost-hollow/reset-sessions', {});
    expect(r.body.released).toBeGreaterThanOrEqual(1);
    expect((await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken })).status).toBe(200);
    const stale = await c.svc('POST', `/internal/characters/${a.char.id}/save`, { sessionId: a.sessionId, rev: 0, state: a.state });
    expect(stale.body.error.code).toBe('session_mismatch');
  });
});
