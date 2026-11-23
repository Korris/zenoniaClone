import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { ObjectId } from 'mongodb';
import { makeCtx } from './helpers.js';

// Card Wager v2: the whole 8-card deck of both sides is staked; the winner takes ONE card of the loser deck. Escrow / settle / refund, idempotency, refusals, kill switch and the
// invariant that no card is ever created or lost (own 8 back + 1 picked for the winner, 7 for the loser, a draw gives everything back).
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
const count = (st) => st.cards.reduce((s, e) => s + e.n, 0);

// two 8-card decks (1 copy of each id)
const DECK_A = ['meadowSlime', 'hornRabbit', 'thornBoar', 'meadowMantis', 'forestWolf', 'brambleTreant', 'thornSprite', 'caveMantis'];
const DECK_B = ['gloomWolf', 'crystalSprite', 'hollowTreant', 'frostSlime', 'snowFox', 'iceImp', 'iceWraith', 'frostBear'];
const own = (deck, extra = []) => [...deck, ...extra].reduce((m, id) => { const e = m.find((x) => x.id === id); if (e) e.n += 1; else m.push(entry(id)); return m; }, []);

/** Makes the owner's account 8 days old (the register call stamps it with the fake clock's now). */
async function agedUser(ctx, o) {
  const doc = await ctx.db.collection('characters').findOne({ _id: new ObjectId(o.char.id) });
  await ctx.db.collection('users').updateOne({ _id: new ObjectId(doc.userId) }, { $set: { createdAt: new Date(ctx.clock.t - 8 * DAY) } });
}

/** An online hero (level 30, account 8 days old) saved with the given cards / deck. */
async function hero(cards, deck, { level = 30, young = false } = {}) {
  const o = await c.online('warrior');
  if (!young) await agedUser(c, o);
  o.state = { ...o.state, level, cards, cardDeck: deck };
  const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state: o.state });
  o.rev = r.body.rev;
  return o;
}
const side = (o) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state });
const escrow = (a, b, id = matchId()) => c.svc('POST', '/internal/card-wagers/escrow', { matchId: id, a: side(a), b: side(b) });
const recv = (o) => ({ sessionId: o.sessionId, rev: o.rev, state: o.state });
const settle = (id, result, pick, a, b) => c.svc('POST', `/internal/card-wagers/${id}/settle`, {
  result, ...(pick ? { pick } : {}), ...(a ? { a: recv(a) } : {}), ...(b ? { b: recv(b) } : {}),
});
const adopt = (o, r) => { o.rev = r.rev; o.state = r.state; };
const adoptBoth = (a, b, r) => { expect(r.status).toBe(200); adopt(a, r.body.a); adopt(b, r.body.b); };
const code = (r) => r.body?.error?.code;
const pair = async () => [await hero(own(DECK_A, ['meadowSlime']), DECK_A), await hero(own(DECK_B, ['frostBear']), DECK_B)];

/** Escrow then settle as a draw, so both heroes stay usable; returns the escrow reply. */
async function playDraw(x, y) {
  const id = matchId();
  const r = await escrow(x, y, id);
  if (r.status !== 200) return r;
  adopt(x, r.body.a); adopt(y, r.body.b);
  const s = await settle(id, 'draw', null, x, y);
  adopt(x, s.body.credited[0]); adopt(y, s.body.credited[1]);
  return r;
}

describe('escrow', () => {
  it('takes the whole deck of both characters, holds it, writes the ledger', async () => {
    const [a, b] = await pair();
    const id = matchId();
    const r = await escrow(a, b, id);
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ replayed: false, status: 'held', a: { rev: a.rev + 1 }, b: { rev: b.rev + 1 } });
    expect(r.body.a.state.cards).toEqual([entry('meadowSlime', 1)]); // the one spare copy stays
    expect(r.body.b.state.cards).toEqual([entry('frostBear', 1)]);
    expect(r.body.a.state.cardDeck).toEqual(DECK_A);
    expect(count((await load(a)).state)).toBe(1);
    const w = await c.db.collection('card_wagers').findOne({ _id: id });
    expect(w).toMatchObject({ status: 'held', a: { characterId: a.char.id, deck: DECK_A }, b: { deck: DECK_B } });
    expect((await c.db.collection('ledger').find({ matchId: id, type: 'card_wager' }).toArray()).map((x) => x.action)).toEqual(['escrow', 'escrow']);
  });

  it('a retry of the same matchId never takes a second deck', async () => {
    const [a, b] = await pair();
    const id = matchId();
    await escrow(a, b, id);
    const again = await escrow(a, b, id);
    expect(again.status).toBe(200);
    expect(again.body).toMatchObject({ replayed: true, status: 'held', a: { rev: a.rev + 1 }, b: { rev: b.rev + 1 } });
    expect(count((await load(a)).state)).toBe(1);
    expect(await c.db.collection('ledger').countDocuments({ matchId: id })).toBe(2);
  });

  it('refuses a short deck, a deck that is not owned, and the Boss / SR+ limits', async () => {
    const b = await hero(own(DECK_B), DECK_B);
    const short = await hero(own(DECK_A), DECK_A.slice(0, 7));
    expect(code(await escrow(short, b))).toBe('wager_deck');
    const poor = await hero(own(DECK_A.slice(0, 7)), DECK_A); // the deck lists a card that is not in the collection
    expect(code(await escrow(poor, b))).toBe('wager_deck');
    const twoBoss = [...DECK_A.slice(0, 6), 'thornfangAlpha', 'frostfangKing'];
    expect(code(await escrow(await hero(own(twoBoss), twoBoss), b))).toBe('wager_deck');
    const threeSr = [...DECK_A.slice(0, 5), 'duneScarab', 'sandRaider', 'amberWisp'];
    expect(code(await escrow(await hero(own(threeSr), threeSr), b))).toBe('wager_deck');
    const fine = [...DECK_A.slice(0, 6), 'duneScarab', 'thornfangAlpha']; // 1 boss + 1 SR is allowed
    const ok = await hero(own(fine), fine);
    expect((await escrow(ok, b)).status).toBe(200);
    expect(count((await load(short)).state)).toBe(8); // refusals change nothing
  });

  it('refuses level < 30, accounts younger than 7 days, the same account, stale rev / session', async () => {
    const [ok, b] = await pair();
    const low = await hero(own(DECK_B), DECK_B, { level: 29 });
    expect(code(await escrow(ok, low))).toBe('wager_level');
    const young = await hero(own(DECK_B), DECK_B, { young: true });
    expect(code(await escrow(ok, young))).toBe('wager_account_age');
    expect(code(await escrow(ok, ok))).toBe('wager_self');
    expect(code(await c.svc('POST', '/internal/card-wagers/escrow', { matchId: matchId(), a: side(ok), b: { ...side(b), rev: 0 } }))).toBe('rev_conflict');
    expect(code(await c.svc('POST', '/internal/card-wagers/escrow', { matchId: matchId(), a: side(ok), b: { ...side(b), sessionId: 'a'.repeat(32) } }))).toBe('session_mismatch');
    expect(count((await load(ok)).state)).toBe(9); // all or nothing
  });

  it('a Locked kind comes back Locked', async () => {
    const cards = own(DECK_A).map((e) => (e.id === 'caveMantis' ? { ...e, locked: 1 } : e));
    const a = await hero(cards, DECK_A);
    const b = await hero(own(DECK_B), DECK_B);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    expect(a.state.cards).toEqual([]);
    const r = await settle(id, 'draw', null, a, b);
    adopt(a, r.body.credited[0]);
    expect(a.state.cards.find((e) => e.id === 'caveMantis')).toMatchObject({ n: 1, locked: 1 });
    expect(a.state.cards.find((e) => e.id === 'hornRabbit')).toMatchObject({ n: 1, locked: 0 });
  });

  it('limits: 2 matches a day per pair, 10 per account', async () => {
    const [a, b] = await pair();
    expect((await playDraw(a, b)).status).toBe(200);
    expect((await playDraw(b, a)).status).toBe(200); // order does not matter for the pair
    expect(code(await playDraw(a, b))).toBe('wager_pair_limit');
    for (let i = 0; i < 8; i++) expect((await playDraw(a, await hero(own(DECK_B), DECK_B))).status).toBe(200); // a: 2 + 8 = 10
    expect(code(await playDraw(a, await hero(own(DECK_B), DECK_B)))).toBe('wager_daily_limit');
    c.clock.t += DAY; // next UTC day
    expect((await playDraw(a, b)).status).toBe(200);
    c.clock.t -= DAY;
  });
});

describe('settle', () => {
  it('winner takes the picked card: own 8 back + 1, loser 7, loser deck trimmed, invariant holds', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    const r = await settle(id, 'a', 'iceWraith', a, b);
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ replayed: false, status: 'settled', result: 'a', pick: 'iceWraith' });
    expect(r.body.credited.map((x) => x.cards.length)).toEqual([9, 7]);
    const [la, lb] = [await load(a), await load(b)];
    expect(count(la.state)).toBe(10); // 8 + the spare + the picked card
    expect(count(lb.state)).toBe(8); // 7 + the spare
    expect(la.state.cards.find((e) => e.id === 'iceWraith')).toMatchObject({ n: 1, locked: 0 });
    expect(la.state.album).toContain(16); // iceWraith is in the Album now
    expect(lb.state.cards.find((e) => e.id === 'iceWraith')).toBeUndefined();
    expect(lb.state.cardDeck).toEqual(DECK_B.filter((x) => x !== 'iceWraith'));
    expect(la.state.cardDeck).toEqual(DECK_A);
    for (const id2 of [...DECK_A, ...DECK_B, 'frostBear']) expect(total([la, lb], id2)).toBe(id2 === 'meadowSlime' || id2 === 'frostBear' ? 2 : 1);
    expect(await held(id)).toBe(0);
    expect((await c.db.collection('ledger').find({ matchId: id }).toArray()).map((x) => x.action).sort()).toEqual(['escrow', 'escrow', 'settle', 'settle']);
  });

  it('a pick of a card that has two copies in the deck leaves the other copy in the loser deck', async () => {
    const dupDeck = [...DECK_B.slice(0, 6), 'frostBear', 'frostBear'];
    const a = await hero(own(DECK_A), DECK_A);
    const b = await hero(own(dupDeck), dupDeck);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    await settle(id, 'a', 'frostBear', a, b);
    const lb = await load(b);
    expect(lb.state.cards.find((e) => e.id === 'frostBear').n).toBe(1);
    expect(lb.state.cardDeck.filter((x) => x === 'frostBear')).toHaveLength(1);
  });

  it('a draw gives everything back and takes no pick; side b can win too', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    expect(code(await settle(id, 'draw', 'iceWraith', a, b))).toBe('wager_pick_invalid');
    const r = await settle(id, 'draw', null, a, b);
    expect(r.status).toBe(200);
    expect(byId((await load(a)).state.cards)).toEqual(byId(own(DECK_A, ['meadowSlime'])));
    expect(byId((await load(b)).state.cards)).toEqual(byId(own(DECK_B, ['frostBear'])));
    adopt(a, r.body.credited[0]); adopt(b, r.body.credited[1]);
    const id2 = matchId();
    adoptBoth(a, b, await escrow(a, b, id2));
    const w = await settle(id2, 'b', 'hornRabbit', a, b);
    expect(w.body.credited.map((x) => x.cards.length)).toEqual([7, 9]);
    expect(count((await load(b)).state)).toBe(10);
  });

  it('refuses a missing pick and a card that is not in the loser staked deck', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    expect(code(await settle(id, 'a', null, a, b))).toBe('wager_pick_required');
    expect(code(await settle(id, 'a', 'meadowSlime', a, b))).toBe('wager_pick_invalid'); // the winner own card
    expect(code(await settle(id, 'a', 'duneScarab', a, b))).toBe('wager_pick_invalid'); // a real card, not staked
    expect((await settle(id, 'a', 'nope', a, b)).status).toBe(400);
    expect(await held(id)).toBe(1); // nothing happened
  });

  it('replay is idempotent, a second pick or result is refused, an unknown match is 404', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    const first = await settle(id, 'a', 'iceWraith', a, b);
    const again = await settle(id, 'a', 'iceWraith', a, b);
    expect(again.status).toBe(200);
    expect(again.body).toMatchObject({ replayed: true, result: 'a', pick: 'iceWraith' });
    expect(again.body.credited[0]).toMatchObject({ side: 'a', rev: first.body.credited[0].rev });
    expect(again.body.credited[0].state.cards).toEqual(first.body.credited[0].state.cards);
    expect(code(await settle(id, 'a', 'frostBear', a, b))).toBe('wager_already_settled'); // a double pick
    expect(code(await settle(id, 'b', 'hornRabbit', a, b))).toBe('wager_already_settled');
    expect((await settle('no-such-match', 'a', 'frostBear')).status).toBe(404);
  });

  it('an offline receiver is written by the API; an online one without state is refused until the zone sends it', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    expect(code(await settle(id, 'a', 'iceWraith', a))).toBe('receiver_online'); // b is still online but sent no state
    expect(await held(id)).toBe(1);
    await c.svc('POST', `/internal/sessions/${b.sessionId}/end`, { characterId: b.char.id });
    const r = await settle(id, 'a', 'iceWraith', a);
    expect(r.status).toBe(200);
    expect((await load(b)).rev).toBe(b.rev + 1);
    expect(count((await load(b)).state)).toBe(8); // 7 back + the spare
    expect(count((await load(a)).state)).toBe(10);
  });

  it('wrong rev / session of an online receiver leaves everything held', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    const post = (body) => c.svc('POST', `/internal/card-wagers/${id}/settle`, body);
    expect(code(await post({ result: 'a', pick: 'iceWraith', a: { ...recv(a), rev: 0 }, b: recv(b) }))).toBe('rev_conflict');
    expect(code(await post({ result: 'a', pick: 'iceWraith', a: { ...recv(a), sessionId: 'b'.repeat(32) }, b: recv(b) }))).toBe('session_mismatch');
    expect(await held(id)).toBe(1);
  });
});

describe('refund', () => {
  it('an expired hold returns both decks at the next redeem of either character, before the lock', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    for (const o of [a, b]) await c.svc('POST', `/internal/sessions/${o.sessionId}/end`, { characterId: o.char.id }); // the zone died, locks gone
    const early = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r0 = await c.svc('POST', '/internal/tickets/redeem', { ticket: early.body.ticket });
    expect(count(r0.body.character.state)).toBe(1); // not expired yet: still held
    await c.svc('POST', `/internal/sessions/${r0.body.sessionId}/end`, { characterId: a.char.id });
    c.clock.t += 1801 * 1000;
    const enter = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: enter.body.ticket });
    expect(r.status).toBe(200);
    expect(byId(r.body.character.state.cards)).toEqual(byId(own(DECK_A, ['meadowSlime'])));
    expect(byId((await load(b)).state.cards)).toEqual(byId(own(DECK_B, ['frostBear']))); // both sides come back together
    expect(await c.db.collection('card_wagers').findOne({ _id: id })).toMatchObject({ status: 'refunded', reason: 'expired' });
    expect((await c.db.collection('ledger').find({ matchId: id }).toArray()).map((x) => x.action).sort()).toEqual(['escrow', 'escrow', 'refund', 'refund']);
    expect(code(await settle(id, 'a', 'iceWraith'))).toBe('wager_refunded');
    c.clock.t -= 1801 * 1000;
  });

  it('reset-sessions refunds every held match of the zone', async () => {
    const [a, b] = await pair();
    adoptBoth(a, b, await escrow(a, b));
    const r = await c.svc('POST', '/internal/zones/frost-hollow/reset-sessions', {});
    expect(r.body.refunded).toBeGreaterThanOrEqual(1);
    expect(count((await load(a)).state)).toBe(9);
    expect(count((await load(b)).state)).toBe(9);
  });

  it('a side with a live session is left held for the next sweep', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    c.clock.t += 1801 * 1000;
    await c.svc('POST', `/internal/characters/${b.char.id}/save`, { sessionId: b.sessionId, rev: b.rev, state: b.state }); // b is alive: fresh heartbeat
    await c.svc('POST', `/internal/sessions/${a.sessionId}/end`, { characterId: a.char.id });
    const enter = await c.call('POST', `/characters/${a.char.id}/enter`, { token: a.user.accessToken });
    const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: enter.body.ticket });
    expect(r.status).toBe(200);
    expect(count(r.body.character.state)).toBe(1);
    expect(await held(id)).toBe(1);
    c.clock.t -= 1801 * 1000;
  });
});

describe('kill switch', () => {
  it('CARD_WAGER_ENABLED=false: escrow is 403 wager_disabled, a held match still settles', async () => {
    const off = await makeCtx({ cfg: { CARD_WAGER_ENABLED: 'false' } });
    try {
      const mk = async () => { const o = await off.online('warrior'); await agedUser(off, o); return o; };
      const [a, b] = [await mk(), await mk()];
      const body = (o) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state });
      const r = await off.svc('POST', '/internal/card-wagers/escrow', { matchId: 'match-off-0001', a: body(a), b: body(b) });
      expect(r.status).toBe(403);
      expect(r.body.error.code).toBe('wager_disabled');
      // a match held before the switch was turned off must still come back
      await off.db.collection('card_wagers').insertOne({
        _id: 'match-held-0001', status: 'held', zoneId: 'frost-hollow', createdAt: new Date(), expiresAt: new Date(Date.now() + 1e6),
        a: { characterId: a.char.id, userId: 'x', deck: DECK_A, lockedIds: [] }, b: { characterId: b.char.id, userId: 'y', deck: DECK_B, lockedIds: [] },
      });
      const s = await off.svc('POST', '/internal/card-wagers/match-held-0001/settle', { result: 'draw', a: { sessionId: a.sessionId, rev: a.rev, state: a.state }, b: { sessionId: b.sessionId, rev: b.rev, state: b.state } });
      expect(s.status).toBe(200);
    } finally { await off.close(); }
  });
});

describe('invariant', () => {
  it('total cards over both characters + held decks never changes through escrow, draw, win and refund', async () => {
    const [a, b] = await pair(); // 9 + 9 cards
    const sum = async () => {
      const chars = [await load(a), await load(b)];
      const rows = await c.db.collection('card_wagers').find({ status: 'held', 'a.characterId': a.char.id }).toArray();
      return chars.reduce((s, ch) => s + count(ch.state), 0) + rows.length * 16;
    };
    expect(await sum()).toBe(18);
    const id1 = matchId();
    adoptBoth(a, b, await escrow(a, b, id1));
    expect(await sum()).toBe(18);
    const d = await settle(id1, 'draw', null, a, b);
    adopt(a, d.body.credited[0]); adopt(b, d.body.credited[1]);
    expect(await sum()).toBe(18);
    const id2 = matchId();
    adoptBoth(a, b, await escrow(a, b, id2));
    expect(await sum()).toBe(18);
    const w = await settle(id2, 'b', 'meadowSlime', a, b);
    adopt(a, w.body.credited[0]); adopt(b, w.body.credited[1]);
    expect(await sum()).toBe(18);
    expect((await load(b)).state.cards.find((e) => e.id === 'meadowSlime').n).toBe(1);
  });
});
