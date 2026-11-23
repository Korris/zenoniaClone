import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';
import { moveTradeGoods } from '../src/rpg/trade-move.js';
import { GEM_SLOTS } from '../src/rpg/rpg-constants.js';
import { armor, makeCtx, weapon } from './helpers.js';

// Soul Gems inside an item (phase 04): trade-locked for good, sockets per slot (weapon 2, armor 2, necklace 1, others 0).
const base = (cls) => ({ ...newCharacterState(cls), version: 6 });
const socketed = (item, sockets) => ({ ...item, sockets });
const necklace = (uid, sockets) => ({ ...armor(uid), slot: 'helmet', templateId: 'helmet-1', sockets });

describe('trade lock of socketed items', () => {
  const give = (inv, uids) => moveTradeGoods(
    { state: { ...base('warrior'), inventory: inv }, giveUids: uids, giveGold: 0 },
    { state: base('wizard'), giveUids: [], giveGold: 0 },
  );

  it('refuses an item holding a gem with trade_socketed', () => {
    const run = () => give([socketed(armor('a1'), ['meadowSlime:p'])], ['a1']);
    expect(run).toThrow(/Soul Gems/);
    try { run(); } catch (e) { expect(e.code).toBe('trade_socketed'); expect(e.status).toBe(422); }
  });

  it('still trades an item with no gems, and an item whose gems were all removed', () => {
    expect(give([armor('a1')], ['a1']).b.inventory.map((i) => i.uid)).toEqual(['a1']);
    expect(give([socketed(armor('a1'), [])], ['a1']).b.inventory.map((i) => i.uid)).toEqual(['a1']);
  });

  it('one socketed item among several spoils the whole trade (nothing moves)', () => {
    expect(() => give([armor('a1'), socketed(weapon('w1'), ['snowFox:g'])], ['a1', 'w1'])).toThrow(/Soul Gems/);
  });
});

describe('sockets per slot', () => {
  const parse = (item) => stateSchema.safeParse({ ...base('warrior'), inventory: [item] });

  it('mirrors GemSockets.SlotCount', () => {
    expect(GEM_SLOTS).toEqual({ weapon: 2, armor: 2, helmet: 1 });
  });

  it('accepts up to the slot limit', () => {
    expect(parse(socketed(weapon('w'), ['meadowSlime:p', 'snowFox:g'])).success).toBe(true);
    expect(parse(socketed(armor('a'), ['meadowSlime:p', 'snowFox:g'])).success).toBe(true);
    expect(parse(necklace('n', ['meadowSlime:p'])).success).toBe(true);
  });

  it('rejects a second gem in a necklace and any gem in a slot without sockets', () => {
    expect(parse(necklace('n', ['meadowSlime:p', 'snowFox:g'])).success).toBe(false);
    for (const slot of ['gloves', 'boots', 'offhand', 'headgear', 'pet']) {
      expect(parse({ ...armor('x'), slot, sockets: ['meadowSlime:p'] }).success).toBe(false);
    }
  });
});

describe('through the API', () => {
  let c;
  beforeAll(async () => { c = await makeCtx(); });
  afterAll(() => c.close());

  it('keeps a socketed item on its owner: the commit answers 422 trade_socketed and nothing moves', async () => {
    const a = await c.online('warrior');
    const b = await c.online('wizard');
    const stateA = { ...a.state, gold: 100, inventory: [socketed(armor('a-armor'), ['meadowSlime:p'])] };
    await c.svc('POST', `/internal/characters/${a.char.id}/save`, { sessionId: a.sessionId, rev: 0, state: stateA });
    const side = (o, state, giveUids, rev) => ({ characterId: o.char.id, sessionId: o.sessionId, rev, state, giveUids, giveGold: 0 });
    const r = await c.svc('POST', '/internal/trades/commit', { tradeId: `gem-${Date.now()}`, a: side(a, stateA, ['a-armor'], 1), b: side(b, b.state, [], 0) });
    expect(r.status).toBe(422);
    expect(r.body.error.code).toBe('trade_socketed');
    const loaded = (await c.call('GET', `/characters/${a.char.id}`, { token: a.user.accessToken })).body.character;
    expect(loaded.state.inventory.map((i) => i.uid)).toEqual(['a-armor']);
  });
});
