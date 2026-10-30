import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { armor, makeCtx, weapon } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const save = (o, state, rev = o.rev, sessionId = o.sessionId) => c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId, rev, state });
const ops = (o, list, rev, reqId = `req-${Math.random().toString(36).slice(2, 10)}`) =>
  c.svc('POST', `/internal/characters/${o.char.id}/ops`, { sessionId: o.sessionId, rev, reqId, ops: list });

describe('save snapshot', () => {
  it('persists the state, bumps rev and round-trips through load', async () => {
    const o = await c.online();
    const state = { ...o.state, level: 5, exp: 120, gold: 77, hp: 55.5, x: 12.5, y: -3, hasPosition: true, inventory: [armor('a-1')], equipment: [weapon('w-1')] };
    const r = await save(o, state);
    expect(r.status).toBe(200);
    expect(r.body.rev).toBe(1);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.rev).toBe(1);
    expect(loaded.level).toBe(5);
    expect(loaded.state).toMatchObject({ level: 5, gold: 77, hp: 55.5, x: 12.5, y: -3, hasPosition: true });
    expect(loaded.state.inventory[0].uid).toBe('a-1');
    expect(loaded.state.equipment[0].uid).toBe('w-1');
  });

  it('round-trips skill ranks + magazine slots and rejects illegal ones', async () => {
    const o = await c.online();
    const skills = { skillRanks: [3, 1, 0, 0, 0, 0, 0, 0], skillSlots: [0, 1, -1, -1, -1, -1, -1, -1] };
    expect((await save(o, { ...o.state, level: 5, ...skills })).status).toBe(200);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.state).toMatchObject(skills);
    const bad = async (patch) => (await save(o, { ...o.state, level: 5, ...patch }, 1)).status;
    expect(await bad({ skillRanks: [3, 3, 0, 0, 0, 0, 0, 0] })).toBe(400); // 6 ranks > level 5
    expect(await bad({ skillRanks: [11, 0, 0, 0, 0, 0, 0, 0] })).toBe(400); // rank > 10
    expect(await bad({ skillRanks: [1, 0, 0, 0, 0, 0, 0, 0, 0] })).toBe(400); // 9 entries
    expect(await bad({ skillRanks: [1, 0], skillSlots: [1, -1] })).toBe(400); // skill 1 not learned
    expect(await bad({ skillRanks: [1, 1], skillSlots: [1, 1] })).toBe(400); // duplicate slot
  });

  it('rejects a stale rev (409 rev_conflict with currentRev) and a wrong session', async () => {
    const o = await c.online();
    expect((await save(o, o.state, 0)).status).toBe(200);
    const stale = await save(o, o.state, 0);
    expect(stale.status).toBe(409);
    expect(stale.body.error).toMatchObject({ code: 'rev_conflict', details: { currentRev: 1 } });
    expect((await save(o, o.state, 1, 'a'.repeat(32))).body.error.code).toBe('session_mismatch');
  });

  it('refreshes the session heartbeat so a saving zone never loses its lock', async () => {
    const o = await c.online();
    for (let i = 0; i < 3; i++) {
      c.clock.t += 60_000;
      expect((await save(o, o.state, i)).status).toBe(200);
    }
    expect((await c.call('POST', `/characters/${o.char.id}/enter`, { token: o.user.accessToken })).status).toBe(409);
  });

  it('validates the payload: caps, duplicates, class change', async () => {
    const o = await c.online();
    const bad = async (patch) => (await save(o, { ...o.state, ...patch })).status;
    expect(await bad({ level: 61 })).toBe(400);
    expect(await bad({ gold: -1 })).toBe(400);
    expect(await bad({ upgradeStone: 10000 })).toBe(400);
    expect(await bad({ x: 1e12 })).toBe(400);
    expect(await bad({ inventory: Array.from({ length: 21 }, (_, i) => armor(`i-${i}`)) })).toBe(400);
    expect(await bad({ inventory: [armor('dup')], equipment: [weapon('dup')] })).toBe(400);
    expect(await bad({ equipment: [weapon('w1'), weapon('w2')] })).toBe(400); // two weapons
    expect(await bad({ inventory: [{ ...armor('z'), rarity: 'mythic' }] })).toBe(400);
    expect(await bad({ inventory: [{ ...armor('z'), enhance: 16 }] })).toBe(400);
    const cls = await save(o, { ...o.state, classId: 'wizard' });
    expect(cls.status).toBe(422);
    expect(cls.body.error.code).toBe('class_mismatch');
  });

  it('refuses snapshots that raise an enhance level (only the enhance endpoint may)', async () => {
    const o = await c.online();
    const r1 = await save(o, { ...o.state, equipment: [weapon('w-1', 0)] });
    expect(r1.status).toBe(200);
    const tamper = await save(o, { ...o.state, equipment: [weapon('w-1', 5)] }, 1);
    expect(tamper.status).toBe(422);
    expect(tamper.body.error.code).toBe('enhance_tamper');
    const fresh = await save(o, { ...o.state, equipment: [weapon('w-1', 0)], inventory: [weapon('w-new', 3)] }, 1);
    expect(fresh.body.error.code).toBe('enhance_tamper'); // a never-seen item must be +0
    expect((await save(o, { ...o.state, equipment: [weapon('w-1', 0)] }, 1)).status).toBe(200);
  });

  it('strips unknown fields', async () => {
    const o = await c.online();
    await save(o, { ...o.state, evil: 'x', inventory: [{ ...armor('a1'), extra: 1 }] });
    const doc = await c.db.collection('characters').findOne({ name: o.char.name });
    expect(doc.state.evil).toBeUndefined();
    expect(doc.state.inventory[0].extra).toBeUndefined();
  });
});

describe('inventory / equipment ops', () => {
  it('adds, equips (swapping the old piece to the bag), unequips, removes, adjusts currency', async () => {
    const o = await c.online();
    let r = await ops(o, [
      { type: 'addItem', item: armor('a1') }, { type: 'addItem', item: weapon('w1') }, { type: 'addItem', item: weapon('w2') },
      { type: 'adjust', gold: 100, upgradeStone: 5, hpPotion: 3 },
    ], 0);
    expect(r.status).toBe(200);
    expect(r.body.rev).toBe(1);
    r = await ops(o, [{ type: 'equip', uid: 'w1' }, { type: 'equip', uid: 'a1' }], 1);
    expect(r.body.state.equipment.map((e) => e.uid).sort()).toEqual(['a1', 'w1']);
    r = await ops(o, [{ type: 'equip', uid: 'w2' }], 2); // swaps w1 back to the bag
    expect(r.body.state.equipment.map((e) => e.uid).sort()).toEqual(['a1', 'w2']);
    expect(r.body.state.inventory.map((e) => e.uid)).toEqual(['w1']);
    r = await ops(o, [{ type: 'unequip', slot: 'armor' }, { type: 'removeItem', uid: 'w1' }], 3);
    expect(r.body.state.inventory.map((e) => e.uid)).toEqual(['a1']);
    expect(r.body.state).toMatchObject({ gold: 100, upgradeStone: 5, hpPotion: 3 });
  });

  it('is all-or-nothing: a failing op leaves the character untouched', async () => {
    const o = await c.online();
    const r = await ops(o, [{ type: 'adjust', gold: 50 }, { type: 'adjust', gold: -60 }], 0);
    expect(r.status).toBe(422);
    expect(r.body.error.code).toBe('insufficient_funds');
    const cur = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(cur).toMatchObject({ rev: 0 });
    expect(cur.state.gold).toBe(0);
  });

  it('enforces rules: wrong class, full bag, duplicates, enhanced grants, unknown items', async () => {
    const o = await c.online('wizard');
    const code = async (list, rev = 0) => (await ops(o, list, rev)).body.error?.code;
    expect(await code([{ type: 'addItem', item: weapon('w1') }, { type: 'equip', uid: 'w1' }])).toBe('wrong_class');
    expect(await code([{ type: 'addItem', item: armor('a1') }, { type: 'addItem', item: armor('a1') }])).toBe('duplicate_item');
    expect(await code([{ type: 'addItem', item: weapon('w9', 2) }])).toBe('enhance_not_allowed');
    expect(await code([{ type: 'removeItem', uid: 'ghost' }])).toBe('item_not_found');
    expect(await code([{ type: 'unequip', slot: 'helmet' }])).toBe('slot_empty');
    const many = Array.from({ length: 21 }, (_, i) => ({ type: 'addItem', item: armor(`m${i}`) }));
    expect(await code(many.slice(0, 20))).toBeUndefined();
    expect(await code([{ type: 'addItem', item: armor('overflow') }], 1)).toBe('inventory_full');
    expect((await ops(o, [], 1)).status).toBe(400);
  });

  it('is idempotent per reqId: a replay returns the stored result without applying twice', async () => {
    const o = await c.online();
    const list = [{ type: 'adjust', gold: 10 }];
    const first = await ops(o, list, 0, 'req-replay-1');
    const replay = await ops(o, list, 0, 'req-replay-1'); // same rev as the lost first response
    expect(first.body.replayed).toBe(false);
    expect(replay.body).toMatchObject({ replayed: true, rev: 1 });
    expect(replay.body.state.gold).toBe(10);
    const stale = await ops(o, list, 0, 'req-replay-2');
    expect(stale.body.error.code).toBe('rev_conflict');
  });
});
