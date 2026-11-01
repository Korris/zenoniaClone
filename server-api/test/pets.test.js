import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { attemptEnhance, findDurabilityTamper } from '../src/rpg/enhance-logic.js';
import { applyOps } from '../src/rpg/inventory-ops.js';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';
import { moveTradeGoods } from '../src/rpg/trade-move.js';
import { armor, makeCtx, weapon } from './helpers.js';

const pet = (uid, templateId = 'pet-spirit', extra = {}) => ({ uid, templateId, slot: 'pet', name: 'Spirit', rarity: 'rare', classId: '', itemLevel: 1, enhance: 0, stats: { atk: 0, def: 0, maxHp: 0, maxMp: 0, critChance: 0, speedMult: 0 }, ...extra });
const base = (classId = 'warrior') => newCharacterState(classId);

describe('the state accepts pets', () => {
  it('a stored character without a pet still loads, and one with a pet keeps it', () => {
    expect(stateSchema.safeParse(base()).success).toBe(true);
    const r = stateSchema.parse({ ...base(), equipment: [weapon('w'), pet('p')], inventory: [pet('p2', 'pet-imp')] });
    expect(r.equipment[1]).toMatchObject({ uid: 'p', slot: 'pet', templateId: 'pet-spirit' });
    expect(r.inventory[0].templateId).toBe('pet-imp');
  });
  it('one pet at a time', () => {
    expect(stateSchema.safeParse({ ...base(), equipment: [pet('p'), pet('p2', 'pet-imp')] }).success).toBe(false);
  });
  it.each([
    ['an enhanced pet', pet('p', 'pet-spirit', { enhance: 1 })],
    ['a pet tied to a class', pet('p', 'pet-spirit', { classId: 'warrior' })],
    ['an unknown pet', pet('p', 'pet-dragon')],
    ['a pet template in another slot', pet('p', 'pet-spirit', { slot: 'armor' })],
    ['the offhand template spirit in the pet slot', pet('p', 'spirit')],
  ])('refuses %s', (_, item) => {
    expect(stateSchema.safeParse({ ...base(), inventory: [item] }).success).toBe(false);
  });
  it('the offhand template called spirit is still a plain offhand', () => {
    expect(stateSchema.safeParse({ ...base('cleric'), equipment: [{ ...weapon('o'), templateId: 'spirit', slot: 'offhand', classId: 'cleric' }] }).success).toBe(true);
  });
});

describe('ops', () => {
  it('any class equips a pet, the old one goes to the bag, unequip brings it back', () => {
    const s = { ...base('assassin'), inventory: [pet('a'), pet('b', 'pet-imp')] };
    const worn = applyOps(s, [{ type: 'equip', uid: 'a' }]);
    expect(worn.equipment.map((e) => e.uid)).toEqual(['a']);
    const swapped = applyOps(worn, [{ type: 'equip', uid: 'b' }]);
    expect(swapped.equipment.map((e) => e.uid)).toEqual(['b']);
    expect(swapped.inventory.map((e) => e.uid)).toEqual(['a']);
    const off = applyOps(swapped, [{ type: 'unequip', slot: 'pet' }]);
    expect(off.equipment).toEqual([]);
    expect(stateSchema.safeParse(off).success).toBe(true);
  });
});

describe('enhance', () => {
  it('refuses a worn pet and one in the bag, and spends nothing', () => {
    for (const s of [{ ...base(), upgradeStone: 10, equipment: [pet('p')] }, { ...base(), upgradeStone: 10, inventory: [pet('p')] }]) {
      const r = attemptEnhance(s, 'p', () => 0);
      expect(r).toMatchObject({ ok: false, success: false, reason: 'not-weapon' });
      expect(r.state.upgradeStone).toBe(10);
    }
  });
});

describe('trade', () => {
  it('a pet in the bag moves to the other class', () => {
    const r = moveTradeGoods(
      { state: { ...base('warrior'), inventory: [pet('p')] }, giveUids: ['p'], giveGold: 0 },
      { state: { ...base('wizard'), gold: 100 }, giveUids: [], giveGold: 50 },
    );
    expect(r.b.inventory.map((i) => i.uid)).toEqual(['p']);
    expect(r.a.inventory).toEqual([]);
    expect(r.a.gold).toBe(50);
  });
  it('a worn pet cannot be given', () => {
    expect(() => moveTradeGoods(
      { state: { ...base('warrior'), equipment: [pet('p')] }, giveUids: ['p'], giveGold: 0 },
      { state: base('wizard'), giveUids: [], giveGold: 0 },
    )).toThrow(/not in the bag/);
  });
});

describe('through the API', () => {
  let c;
  beforeAll(async () => { c = await makeCtx(); });
  afterAll(() => c.close());

  const save = (o, state, rev) => c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev, state });

  it('a snapshot with a bought pet saves, loads back, and the enhance route refuses it', async () => {
    const o = await c.online();
    const state = { ...o.state, upgradeStone: 3, equipment: [pet('p')], inventory: [armor('a')] };
    expect((await save(o, state, o.rev)).status).toBe(200);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.state.equipment[0]).toMatchObject({ uid: 'p', slot: 'pet', templateId: 'pet-spirit', enhance: 0 });
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/enhance`, { sessionId: o.sessionId, rev: loaded.rev, reqId: 'pet-enhance-1', itemUid: 'p' });
    expect(r.status).toBe(200);
    expect(r.body).toMatchObject({ ok: false, success: false, reason: 'not-weapon' });
    const after = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(after.state.upgradeStone).toBe(3);
    expect(after.state.equipment[0].enhance).toBe(0);
  });

  it('a snapshot with an enhanced pet is refused', async () => {
    const o = await c.online();
    const r = await save(o, { ...o.state, inventory: [pet('p', 'pet-imp', { enhance: 3 })] }, o.rev);
    expect(r.status).toBeGreaterThanOrEqual(400);
    expect(r.status).toBeLessThan(500);
  });
});

describe('durability', () => {
  it('is 0..max, and a pet without the field counts as full', () => {
    expect(stateSchema.parse({ ...base(), inventory: [pet('p')] }).inventory[0].durability).toBe(1000);
    expect(stateSchema.parse({ ...base(), inventory: [pet('p', 'pet-imp', { durability: 12 })] }).inventory[0].durability).toBe(12);
    expect(stateSchema.safeParse({ ...base(), inventory: [pet('p', 'pet-imp', { durability: 1001 })] }).success).toBe(false);
    expect(stateSchema.safeParse({ ...base(), inventory: [pet('p', 'pet-imp', { durability: -1 })] }).success).toBe(false);
    expect(stateSchema.parse({ ...base(), inventory: [armor('a')] }).inventory[0].durability).toBe(0);
  });
  it('a saved value above the stored one is tamper, equal or lower is not, an old stored pet counts as full', () => {
    const with_ = (d) => ({ ...base(), equipment: [pet('p', 'pet-spirit', { durability: d })] });
    expect(findDurabilityTamper(with_(500), with_(501))).toBe('p');
    expect(findDurabilityTamper(with_(500), with_(500))).toBeNull();
    expect(findDurabilityTamper(with_(500), with_(499))).toBeNull();
    const oldStored = { ...base(), inventory: [pet('p')] };
    delete oldStored.inventory[0].durability;
    expect(findDurabilityTamper(oldStored, with_(1000))).toBeNull();
  });
  it('a pet the API has not seen may already be drained: the zone can hit it before its first save', () => {
    expect(findDurabilityTamper(base(), { ...base(), equipment: [pet('new', 'pet-spirit', { durability: 998 })] })).toBeNull();
  });
  it('ops: a granted pet starts full', () => {
    expect(applyOps(base(), [{ type: 'addItem', item: stateSchema.parse({ ...base(), inventory: [pet('p')] }).inventory[0] }]).inventory[0].durability).toBe(1000);
    expect(() => applyOps(base(), [{ type: 'addItem', item: { ...pet('p'), durability: 3 } }])).toThrow(/full durability/);
  });
  it('a trade keeps what the pet has left', () => {
    const r = moveTradeGoods(
      { state: { ...base('warrior'), inventory: [pet('p', 'pet-imp', { durability: 37 })] }, giveUids: ['p'], giveGold: 0 },
      { state: { ...base('wizard') }, giveUids: [], giveGold: 0 },
    );
    expect(r.b.inventory[0].durability).toBe(37);
  });
});

describe('durability through the API', () => {
  let c;
  beforeAll(async () => { c = await makeCtx(); });
  afterAll(() => c.close());
  const save = (o, state, rev) => c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev, state });

  it('a save above the stored value gets durability_tamper and changes nothing; going down and losing the pet are fine', async () => {
    const o = await c.online();
    const at = (d) => ({ ...o.state, version: 2, equipment: [pet('p', 'pet-spirit', { durability: d })] });
    expect((await save(o, at(500), o.rev)).status).toBe(200);
    const up = await save(o, at(501), o.rev + 1);
    expect(up.status).toBe(422);
    expect(up.body.error.code).toBe('durability_tamper');
    expect((await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character.state.equipment[0].durability).toBe(500);
    expect((await save(o, at(499), o.rev + 1)).status).toBe(200);
    expect((await save(o, { ...o.state, version: 2, equipment: [] }, o.rev + 2)).status).toBe(200);
    expect((await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character.state.equipment).toEqual([]);
  });

  it('a trade with a raised pet in the state is refused the same way', async () => {
    const a = await c.online('warrior');
    const b = await c.online('wizard');
    const stateA = { ...a.state, gold: 10, inventory: [pet('p', 'pet-imp', { durability: 200 })] };
    await save(a, stateA, 0);
    const raised = { ...stateA, inventory: [pet('p', 'pet-imp', { durability: 900 })] };
    const r = await c.svc('POST', '/internal/trades/commit', {
      tradeId: `pet-trade-${Date.now()}`,
      a: { characterId: a.char.id, sessionId: a.sessionId, rev: 1, state: raised, giveUids: ['p'], giveGold: 0 },
      b: { characterId: b.char.id, sessionId: b.sessionId, rev: 0, state: b.state, giveUids: [], giveGold: 0 },
    });
    expect(r.status).toBe(422);
    expect(r.body.error.code).toBe('durability_tamper');
  });
});
