import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { ObjectId } from 'mongodb';
import { makeCtx } from './helpers.js';

// Card Wager escrow: escrow / settle / refund, idempotency, refusals, kill switch, and the invariant that no card is ever created or lost.
let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const DAY = 86_400_000;
let seq = 0;
const matchId = () => `match-${Date.now()}-${++seq}`;
const entry = (id, n = 1, locked = 0) => ({ id, n, locked });
const load = async (o) => (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
const total = (chars, id) => chars.reduce((s, ch) => s + (ch.state.cards.find((e) => e.id === id)?.n ?? 0), 0);
const held = (mid) => c.db.collection('card_wagers').countDocuments({ _id: mid, status: 'held' });
const byId = (cards) => [...cards].sort((x, y) => x.id.localeCompare(y.id));

/** Makes the owner's account 8 days old (the register call stamps it with the fake clock's now). */
async function agedUser(ctx, o) {
  const doc = await ctx.db.collection('characters').findOne({ _id: new ObjectId(o.char.id) });
  await ctx.db.collection('users').updateOne({ _id: new ObjectId(doc.userId) }, { $set: { createdAt: new Date(ctx.clock.t - 8 * DAY) } });
}

/** An online hero (level 30, account 8 days old) saved with the given cards / deck. */
async function hero(cards, { deck = [], level = 30, young = false } = {}) {
  const o = await c.online('warrior');
  if (!young) await agedUser(c, o);
  o.state = { ...o.state, level, cards, cardDeck: deck };
  const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state: o.state });
  o.rev = r.body.rev;
  return o;
}
const side = (o, cardId, over = {}) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state, cardId, ...over });
const escrow = (a, ca, b, cb, id = matchId(), over = {}) => c.svc('POST', '/internal/card-wagers/escrow', { matchId: id, a: side(a, ca, over.a), b: side(b, cb, over.b) });
const settle = (id, result, a, b) => c.svc('POST', `/internal/card-wagers/${id}/settle`, {
  result, ...(a ? { a: { sessionId: a.sessionId, rev: a.rev, state: a.state } } : {}), ...(b ? { b: { sessionId: b.sessionId, rev: b.rev, state: b.state } } : {}),
});
const adopt = (o, r) => { o.rev = r.rev; o.state = r.state; };
const code = (r) => r.body?.error?.code;

describe('escrow', () => {
  it('takes the card from both characters, holds it, writes the ledger', async () => {
    const a = await hero([entry('meadowSlime', 2)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    const r = await escrow(a, 'meadowSlime', b, 'hornRabbit', id);
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ replayed: false, status: 'held', a: { rev: a.rev + 1 }, b: { rev: b.rev + 1 } });
    expect(r.body.a.state.cards).toEqual([entry('meadowSlime', 1)]);
    expect(r.body.b.state.cards).toEqual([]); // the last copy leaves; the Album keeps the card
    expect((await load(a)).state.cards).toEqual([entry('meadowSlime', 1)]);
    const w = await c.db.collection('card_wagers').findOne({ _id: id });
    expect(w).toMatchObject({ status: 'held', a: { characterId: a.char.id, cardId: 'meadowSlime' }, b: { cardId: 'hornRabbit' } });
    const rows = await c.db.collection('ledger').find({ matchId: id, type: 'card_wager' }).toArray();
    expect(rows.map((x) => x.action)).toEqual(['escrow', 'escrow']);
  });

  it('a retry of the same matchId never takes a second card', async () => {
    const a = await hero([entry('meadowSlime', 3)]);
    const b = await hero([entry('hornRabbit', 3)]);
    const id = matchId();
    await escrow(a, 'meadowSlime', b, 'hornRabbit', id);
    const again = await escrow(a, 'meadowSlime', b, 'hornRabbit', id);
    expect(again.status).toBe(200);
    expect(again.body).toMatchObject({ replayed: true, status: 'held', a: { rev: a.rev + 1 }, b: { rev: b.rev + 1 } });
    expect((await load(a)).state.cards).toEqual([entry('meadowSlime', 2)]);
    expect(await c.db.collection('ledger').countDocuments({ matchId: id })).toBe(2);
  });

  it('refuses SR / SSR / boss, different rarity, unknown card', async () => {
    const a = await hero([entry('meadowSlime'), entry('duneScarab'), entry('frostfangKing'), entry('gloomWolf')]);
    const b = await hero([entry('hornRabbit'), entry('duneScarab'), entry('frostfangKing'), entry('gloomWolf')]);
    expect(code(await escrow(a, 'duneScarab', b, 'duneScarab'))).toBe('wager_rarity');
    expect(code(await escrow(a, 'frostfangKing', b, 'frostfangKing'))).toBe('wager_boss_card');
    expect(code(await escrow(a, 'meadowSlime', b, 'gloomWolf'))).toBe('wager_rarity_mismatch');
    expect((await escrow(a, 'meadowSlime', b, 'hornRabbit', matchId(), { a: { cardId: 'nope' } })).status).toBe(400);
    expect((await load(a)).rev).toBe(a.rev);
  });

  it('refuses a locked kind, deck copies and unowned cards (spare only)', async () => {
    const a = await hero([entry('meadowSlime', 3, 1), entry('thornBoar', 1), entry('forestWolf', 1), entry('snowFox', 2)], { deck: ['meadowSlime', 'thornBoar', 'snowFox'] });
    const b = await hero([entry('hornRabbit', 3)]);
    expect(code(await escrow(a, 'meadowSlime', b, 'hornRabbit'))).toBe('wager_no_spare'); // the kind is locked: no copy is spare, deck or not
    expect(code(await escrow(a, 'thornBoar', b, 'hornRabbit'))).toBe('wager_no_spare'); // the only copy is in the deck
    expect(code(await escrow(a, 'caveMantis', b, 'hornRabbit'))).toBe('wager_no_spare'); // not owned
    expect((await escrow(a, 'forestWolf', b, 'hornRabbit')).status).toBe(200);
  });

  it('refuses level < 30, accounts younger than 7 days, the same account, stale rev / session', async () => {
    const ok = await hero([entry('meadowSlime', 2)]);
    const low = await hero([entry('hornRabbit', 2)], { level: 29 });
    expect(code(await escrow(ok, 'meadowSlime', low, 'hornRabbit'))).toBe('wager_level');
    const young = await hero([entry('hornRabbit', 2)], { young: true });
    expect(code(await escrow(ok, 'meadowSlime', young, 'hornRabbit'))).toBe('wager_account_age');
    expect(code(await escrow(ok, 'meadowSlime', ok, 'meadowSlime'))).toBe('wager_self');
    const b = await hero([entry('hornRabbit', 2)]);
    expect(code(await escrow(ok, 'meadowSlime', b, 'hornRabbit', matchId(), { b: { rev: 0 } }))).toBe('rev_conflict');
    expect(code(await escrow(ok, 'meadowSlime', b, 'hornRabbit', matchId(), { b: { sessionId: 'a'.repeat(32) } }))).toBe('session_mismatch');
    expect((await load(ok)).state.cards).toEqual([entry('meadowSlime', 2)]); // all or nothing
  });

  it('limits: 3 wager matches a day per account, 2 per pair', async () => {
    const a = await hero([entry('meadowSlime', 9)]);
    const b = await hero([entry('hornRabbit', 9)]);
    const d = await hero([entry('thornBoar', 9)]);
    const e = await hero([entry('forestWolf', 9)]);
    const go = async (x, cx, y, cy) => { const r = await escrow(x, cx, y, cy); if (r.status === 200) { adopt(x, r.body.a); adopt(y, r.body.b); } return r; };
    expect((await go(a, 'meadowSlime', b, 'hornRabbit')).status).toBe(200);
    expect((await go(b, 'hornRabbit', a, 'meadowSlime')).status).toBe(200); // order does not matter for the pair
    expect(code(await go(a, 'meadowSlime', b, 'hornRabbit'))).toBe('wager_pair_limit');
    expect((await go(a, 'meadowSlime', d, 'thornBoar')).status).toBe(200); // a: 3rd of the day
    expect(code(await go(a, 'meadowSlime', e, 'forestWolf'))).toBe('wager_daily_limit');
    c.clock.t += DAY; // next UTC day
    expect((await go(a, 'meadowSlime', e, 'forestWolf')).status).toBe(200);
    c.clock.t -= DAY;
  });
});

describe('settle', () => {
  it('winner online gets both cards, loser keeps nothing extra, invariant holds', async () => {
    const a = await hero([entry('meadowSlime', 2)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    const r = await settle(id, 'a', a, null); // the loser b went offline: nothing to send for it
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ replayed: false, status: 'settled', result: 'a', credited: [{ side: 'a', cards: ['meadowSlime', 'hornRabbit'], rev: a.rev + 1 }] });
    const [la, lb] = [await load(a), await load(b)];
    expect(byId(la.state.cards)).toEqual([entry('hornRabbit', 1), entry('meadowSlime', 2)]);
    expect(la.state.album).toContain(1); // hornRabbit is now in the Album
    expect(lb.state.cards).toEqual([]);
    expect(await held(id)).toBe(0);
    expect(total([la, lb], 'meadowSlime')).toBe(2);
    const actions = (await c.db.collection('ledger').find({ matchId: id }).toArray()).map((x) => x.action).sort();
    expect(actions).toEqual(['escrow', 'escrow', 'settle']);
  });

  it('draw gives each side its own card back', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    const r = await settle(id, 'draw', a, b);
    expect(r.status).toBe(200);
    expect((await load(a)).state.cards).toEqual([entry('meadowSlime', 1)]);
    expect((await load(b)).state.cards).toEqual([entry('hornRabbit', 1)]);
  });

  it('winner offline: the API writes the character itself; online without state is refused until the zone sends it', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    const blind = await settle(id, 'b'); // b is still online but the zone sent no state
    expect(code(blind)).toBe('receiver_online');
    expect(await held(id)).toBe(1); // nothing happened
    await c.svc('POST', `/internal/sessions/${b.sessionId}/end`, { characterId: b.char.id });
    const r = await settle(id, 'b');
    expect(r.status).toBe(200);
    expect(r.body.credited[0]).toMatchObject({ side: 'b', rev: b.rev + 1 });
    expect(byId((await load(b)).state.cards)).toEqual([entry('hornRabbit', 1), entry('meadowSlime', 1)]);
    expect((await load(b)).rev).toBe(b.rev + 1);
  });

  it('replay is idempotent, a different result is refused, an unknown match is 404', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    const first = await settle(id, 'a', a);
    const again = await settle(id, 'a', a);
    expect(again.status).toBe(200);
    expect(again.body).toMatchObject({ replayed: true, result: 'a', credited: [{ side: 'a', rev: first.body.credited[0].rev }] });
    expect(again.body.credited[0].state.cards).toEqual(first.body.credited[0].state.cards);
    expect(code(await settle(id, 'b', null, b))).toBe('wager_already_settled');
    expect(byId((await load(a)).state.cards)).toEqual([entry('hornRabbit', 1), entry('meadowSlime', 1)]);
    expect((await settle('no-such-match', 'a')).status).toBe(404);
  });

  it('wrong rev / session of the online receiver leaves everything held', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    expect(code(await settle(id, 'a', { ...a, rev: 0 }))).toBe('rev_conflict');
    expect(code(await settle(id, 'a', { ...a, sessionId: 'b'.repeat(32) }))).toBe('session_mismatch');
    expect(await held(id)).toBe(1);
  });
});

describe('refund', () => {
  it('an expired hold is returned at the next redeem of either character, before the lock', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    for (const o of [a, b]) await c.svc('POST', `/internal/sessions/${o.sessionId}/end`, { characterId: o.char.id }); // the zone died, locks gone
    const early = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r0 = await c.svc('POST', '/internal/tickets/redeem', { ticket: early.body.ticket });
    expect(r0.body.character.state.cards).toEqual([]); // not expired yet: still held
    await c.svc('POST', `/internal/sessions/${r0.body.sessionId}/end`, { characterId: a.char.id });
    c.clock.t += 1801 * 1000;
    const enter = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: enter.body.ticket });
    expect(r.status).toBe(200);
    expect(r.body.character.state.cards).toEqual([entry('meadowSlime', 1)]);
    expect((await load(b)).state.cards).toEqual([entry('hornRabbit', 1)]); // both sides come back together
    expect(await c.db.collection('card_wagers').findOne({ _id: id })).toMatchObject({ status: 'refunded', reason: 'expired' });
    expect((await c.db.collection('ledger').find({ matchId: id }).toArray()).map((x) => x.action).sort()).toEqual(['escrow', 'escrow', 'refund', 'refund']);
    expect(code(await settle(id, 'a'))).toBe('wager_refunded');
    c.clock.t -= 1801 * 1000;
  });

  it('reset-sessions refunds every held wager of the zone', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit'));
    const r = await c.svc('POST', '/internal/zones/frost-hollow/reset-sessions', {});
    expect(r.body.refunded).toBeGreaterThanOrEqual(1);
    expect((await load(a)).state.cards).toEqual([entry('meadowSlime', 1)]);
    expect((await load(b)).state.cards).toEqual([entry('hornRabbit', 1)]);
  });

  it('a side with a live session is left held for the next sweep', async () => {
    const a = await hero([entry('meadowSlime', 1)]);
    const b = await hero([entry('hornRabbit', 1)]);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id));
    c.clock.t += 1801 * 1000;
    await c.svc('POST', `/internal/characters/${b.char.id}/save`, { sessionId: b.sessionId, rev: b.rev, state: b.state }); // b is alive: fresh heartbeat
    await c.svc('POST', `/internal/sessions/${a.sessionId}/end`, { characterId: a.char.id });
    const enter = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: enter.body.ticket });
    expect(r.status).toBe(200);
    expect(r.body.character.state.cards).toEqual([]);
    expect(await held(id)).toBe(1);
    c.clock.t -= 1801 * 1000;
  });
});

describe('kill switch', () => {
  it('CARD_WAGER_ENABLED=false: escrow is 403 wager_disabled, a held wager still settles', async () => {
    const off = await makeCtx({ cfg: { CARD_WAGER_ENABLED: 'false' } });
    try {
      const mk = async () => {
        const o = await off.online('warrior');
        await agedUser(off, o);
        return o;
      };
      const [a, b] = [await mk(), await mk()];
      const body = (o, cardId) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state, cardId });
      const r = await off.svc('POST', '/internal/card-wagers/escrow', { matchId: 'match-off-0001', a: body(a, 'meadowSlime'), b: body(b, 'hornRabbit') });
      expect(r.status).toBe(403);
      expect(r.body.error.code).toBe('wager_disabled');
      // a wager held before the switch was turned off must still come back
      await off.db.collection('card_wagers').insertOne({
        _id: 'match-held-0001', status: 'held', zoneId: 'frost-hollow', createdAt: new Date(), expiresAt: new Date(Date.now() + 1e6),
        a: { characterId: a.char.id, userId: 'x', cardId: 'meadowSlime' }, b: { characterId: b.char.id, userId: 'y', cardId: 'hornRabbit' },
      });
      const s = await off.svc('POST', '/internal/card-wagers/match-held-0001/settle', { result: 'draw', a: { sessionId: a.sessionId, rev: a.rev, state: a.state }, b: { sessionId: b.sessionId, rev: b.rev, state: b.state } });
      expect(s.status).toBe(200);
    } finally { await off.close(); }
  });
});

describe('invariant', () => {
  it('total cards over both characters + held rows never changes through escrow, draw, win and refund', async () => {
    const a = await hero([entry('meadowSlime', 4)]);
    const b = await hero([entry('hornRabbit', 4)]);
    const sum = async () => {
      const chars = [await load(a), await load(b)];
      const heldRows = await c.db.collection('card_wagers').find({ status: 'held', 'a.characterId': a.char.id }).toArray();
      return total(chars, 'meadowSlime') + total(chars, 'hornRabbit') + heldRows.length * 2;
    };
    expect(await sum()).toBe(8);
    const id1 = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id1));
    expect(await sum()).toBe(8);
    const d = await settle(id1, 'draw', a, b);
    adopt(a, d.body.credited[0]); adopt(b, d.body.credited[1]);
    expect(await sum()).toBe(8);
    const id2 = matchId();
    adoptBoth(a, b, await escrow(a, 'meadowSlime', b, 'hornRabbit', id2));
    expect(await sum()).toBe(8);
    const w = await settle(id2, 'b', null, b);
    adopt(b, w.body.credited[0]);
    expect(await sum()).toBe(8);
    expect((await load(b)).state.cards.find((e) => e.id === 'meadowSlime').n).toBe(1);
  });
});

function adoptBoth(a, b, r) {
  expect(r.status).toBe(200);
  adopt(a, r.body.a);
  adopt(b, r.body.b);
}
