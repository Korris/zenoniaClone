import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { ObjectId } from 'mongodb';
import { makeCtx } from './helpers.js';

// Card Wager hardening (review 23/11 phase 06): per-day counters that cannot skew, the periodic sweep of expired holds, deleting a character with a held match,
// orphaned matches, replays with the kill switch off, and the Lock flag. The core escrow / settle behaviour is in card-wager.test.js.
let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const DAY = 86_400_000;
const HOLD = 1801 * 1000;
let seq = 0;
const matchId = () => `hard-${Date.now()}-${++seq}`;
const entry = (id, n = 1, locked = 0) => ({ id, n, locked });
const load = async (o) => (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
const held = (mid) => c.db.collection('card_wagers').countDocuments({ _id: mid, status: 'held' });
const count = (st) => st.cards.reduce((s, e) => s + e.n, 0);
const DECK_A = ['meadowSlime', 'hornRabbit', 'thornBoar', 'meadowMantis', 'forestWolf', 'brambleTreant', 'thornSprite', 'caveMantis'];
const DECK_B = ['gloomWolf', 'crystalSprite', 'hollowTreant', 'frostSlime', 'snowFox', 'iceImp', 'iceWraith', 'frostBear'];
const own = (deck, extra = []) => [...deck, ...extra].reduce((m, id) => { const e = m.find((x) => x.id === id); if (e) e.n += 1; else m.push(entry(id)); return m; }, []);

async function ageUser(o) {
  const doc = await c.db.collection('characters').findOne({ _id: new ObjectId(o.char.id) });
  await c.db.collection('users').updateOne({ _id: new ObjectId(doc.userId) }, { $set: { createdAt: new Date(c.clock.t - 8 * DAY) } });
}

async function hero(cards, deck) {
  const o = await c.online('warrior');
  await ageUser(o);
  o.state = { ...o.state, level: 30, cards, cardDeck: deck };
  o.rev = (await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state: o.state })).body.rev;
  return o;
}
const side = (o) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state });
const escrow = (a, b, id = matchId()) => c.svc('POST', '/internal/card-wagers/escrow', { matchId: id, a: side(a), b: side(b) });
const recv = (o) => ({ sessionId: o.sessionId, rev: o.rev, state: o.state });
const settle = (id, result, pick, a, b) => c.svc('POST', `/internal/card-wagers/${id}/settle`, { result, ...(pick ? { pick } : {}), ...(a ? { a: recv(a) } : {}), ...(b ? { b: recv(b) } : {}) });
const adopt = (o, r) => { o.rev = r.rev; o.state = r.state; };
const adoptBoth = (a, b, r) => { expect(r.status).toBe(200); adopt(a, r.body.a); adopt(b, r.body.b); };
const code = (r) => r.body?.error?.code;
const pair = async () => [await hero(own(DECK_A, ['meadowSlime']), DECK_A), await hero(own(DECK_B, ['frostBear']), DECK_B)];
const offline = (...os) => Promise.all(os.map((o) => c.svc('POST', `/internal/sessions/${o.sessionId}/end`, { characterId: o.char.id })));
const userOf = async (o) => (await c.db.collection('characters').findOne({ _id: new ObjectId(o.char.id) })).userId;
const dayKey = (userId) => `u:${userId}:${Math.floor(c.clock.t / DAY)}`;

/** A second hero of the same account, online, with the given cards and deck. */
async function sibling(o, cards, deck) {
  const ch = await c.createChar(o.user.accessToken, `sib${++seq}x${Date.now() % 100000}`);
  const e = await c.call('POST', `/characters/${ch.id}/enter`, { token: o.user.accessToken });
  const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: e.body.ticket });
  const s = { user: o.user, char: ch, sessionId: r.body.sessionId, rev: r.body.character.rev, state: { ...r.body.character.state, level: 30, cards, cardDeck: deck } };
  s.rev = (await c.svc('POST', `/internal/characters/${ch.id}/save`, { sessionId: s.sessionId, rev: s.rev, state: s.state })).body.rev;
  return s;
}

describe('limit counters', () => {
  it('two escrows of one account at the same instant cannot both take the last slot of the day', async () => {
    const x1 = await hero(own(DECK_A), DECK_A);
    const x2 = await sibling(x1, own(DECK_A), DECK_A);
    const y1 = await hero(own(DECK_B), DECK_B);
    const y2 = await hero(own(DECK_B), DECK_B);
    const userId = await userOf(x1);
    await c.db.collection('card_wager_counters').updateOne({ _id: dayKey(userId) }, { $set: { n: 9, at: new Date() } }, { upsert: true }); // 9 of 10 used
    const [r1, r2] = await Promise.all([escrow(x1, y1), escrow(x2, y2)]);
    expect([r1.status, r2.status].sort()).toEqual([200, 422]);
    expect(code(r1.status === 422 ? r1 : r2)).toBe('wager_daily_limit');
    expect((await c.db.collection('card_wager_counters').findOne({ _id: dayKey(userId) })).n).toBe(10); // the refused try rolled its increments back
    const refused = r1.status === 422 ? [x1, y1] : [x2, y2];
    expect(count((await load(refused[0])).state)).toBe(8); // all or nothing: nobody lost a card to the refused escrow
    expect(count((await load(refused[1])).state)).toBe(8);
  });

  it('a refunded match gives its slot back', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    const key = dayKey(await userOf(a));
    expect((await c.db.collection('card_wager_counters').findOne({ _id: key })).n).toBe(1);
    await offline(a, b);
    expect(await c.app.cardWagers.refundOne(id, 'test')).toBe(true);
    expect((await c.db.collection('card_wager_counters').findOne({ _id: key })).n).toBe(0);
  });
});

describe('sweep of expired holds', () => {
  it('refunds an expired match whose players are both offline, with no redeem needed', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    await offline(a, b);
    expect((await c.app.cardWagers.sweepExpired()).refunded).toBe(0); // not expired yet
    expect(await held(id)).toBe(1);
    c.clock.t += HOLD;
    const r = await c.app.cardWagers.sweepExpired();
    c.clock.t -= HOLD;
    expect(r.refunded).toBeGreaterThanOrEqual(1);
    expect(count((await load(a)).state)).toBe(9);
    expect(count((await load(b)).state)).toBe(9);
    expect(await c.db.collection('card_wagers').findOne({ _id: id })).toMatchObject({ status: 'refunded', reason: 'expired' });
  });

  it('leaves a match held while a player is online; a settle that comes after the refund is refused and moves no card', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    c.clock.t += HOLD;
    await c.svc('POST', `/internal/characters/${b.char.id}/save`, { sessionId: b.sessionId, rev: b.rev, state: b.state }); // b keeps its heartbeat
    await offline(a);
    await c.app.cardWagers.sweepExpired();
    expect(await held(id)).toBe(1); // b is alive
    await offline(b);
    await c.app.cardWagers.sweepExpired();
    expect(await held(id)).toBe(0);
    expect(code(await settle(id, 'a', 'iceWraith'))).toBe('wager_refunded'); // the settle that comes too late
    c.clock.t -= HOLD;
    expect(count((await load(a)).state)).toBe(9);
    expect(count((await load(b)).state)).toBe(9); // neither the refund nor the late settle created a card
  });

  it('a settle that landed first is never refunded by a later sweep', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    expect((await settle(id, 'a', 'iceWraith', a, b)).status).toBe(200);
    await offline(a, b);
    c.clock.t += HOLD;
    await c.app.cardWagers.sweepExpired();
    c.clock.t -= HOLD;
    expect(count((await load(a)).state)).toBe(10);
    expect(count((await load(b)).state)).toBe(8);
  });
});

describe('a character with a held match', () => {
  it('cannot be deleted while the match is held (409 wager_held), and can after the refund', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    await offline(a);
    const del = await c.call('DELETE', `/characters/${a.char.id}`, { token: a.user.accessToken });
    expect(del.status).toBe(409);
    expect(del.body.error.code).toBe('wager_held');
    expect(await held(id)).toBe(1);
    await offline(b);
    expect(await c.app.cardWagers.refundOne(id, 'test')).toBe(true);
    expect((await c.call('DELETE', `/characters/${a.char.id}`, { token: a.user.accessToken })).status).toBe(200);
  });

  it('an orphaned match (one character gone) refunds the side that exists, and settles to it', async () => {
    const [a, b] = await pair();
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    await offline(a, b);
    await c.db.collection('characters').deleteOne({ _id: new ObjectId(a.char.id) }); // removed behind the API's back
    expect(await c.app.cardWagers.refundOne(id, 'orphan')).toBe(true);
    expect(count((await load(b)).state)).toBe(9); // b got its deck back although a is gone
    const [a2, b2] = await pair();
    const id2 = matchId();
    adoptBoth(a2, b2, await escrow(a2, b2, id2));
    await offline(a2);
    await c.db.collection('characters').deleteOne({ _id: new ObjectId(a2.char.id) });
    const s = await settle(id2, 'b', 'hornRabbit', null, b2);
    expect(s.status).toBe(200);
    expect(s.body.credited.map((x) => x.side)).toEqual(['b']); // only the side that exists
    expect(count((await load(b2)).state)).toBe(10); // the 8 own cards + the picked card + the spare
  });
});

describe('kill switch and the Lock flag', () => {
  it('a replay of an existing match is answered even when the switch is off now', async () => {
    const off = await makeCtx({ cfg: { CARD_WAGER_ENABLED: 'false' } });
    try {
      const mk = async () => { const o = await off.online('warrior'); return o; };
      const [a, b] = [await mk(), await mk()];
      await off.db.collection('card_wagers').insertOne({
        _id: 'match-held-replay', status: 'held', zoneId: 'frost-hollow', createdAt: new Date(), expiresAt: new Date(Date.now() + 1e6),
        a: { characterId: a.char.id, userId: 'x', deck: DECK_A, lockedIds: [] }, b: { characterId: b.char.id, userId: 'y', deck: DECK_B, lockedIds: [] },
      });
      const body = (o) => ({ characterId: o.char.id, sessionId: o.sessionId, rev: o.rev, state: o.state });
      const r = await off.svc('POST', '/internal/card-wagers/escrow', { matchId: 'match-held-replay', a: body(a), b: body(b) });
      expect(r.status).toBe(200);
      expect(r.body).toMatchObject({ replayed: true, status: 'held' });
      const fresh = await off.svc('POST', '/internal/card-wagers/escrow', { matchId: 'match-new', a: body(a), b: body(b) });
      expect(fresh.status).toBe(403);
    } finally { await off.close(); }
  });

  it('the Lock flag survives a copy that dropped during the match', async () => {
    const cards = own(DECK_A).map((e) => (e.id === 'caveMantis' ? { ...e, locked: 1 } : e));
    const a = await hero(cards, DECK_A);
    const b = await hero(own(DECK_B), DECK_B);
    const id = matchId();
    adoptBoth(a, b, await escrow(a, b, id));
    a.state = { ...a.state, cards: [entry('caveMantis', 1, 0)] }; // a new, unlocked copy dropped while the deck was held
    const r = await settle(id, 'draw', null, a, b);
    adopt(a, r.body.credited[0]);
    expect(a.state.cards.find((e) => e.id === 'caveMantis')).toMatchObject({ n: 2, locked: 1 });
  });
});
