import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

let c, a, b;
beforeAll(async () => { c = await makeCtx(); a = await c.register('alice'); b = await c.register('bobby'); });
afterAll(() => c.close());

const create = (token, name, classId) => c.call('POST', '/characters', { token, body: { name, classId } });

describe('character rules', () => {
  it('creates a level-1 character with a SaveData-shaped state', async () => {
    const r = await create(a.accessToken, 'Aria', 'wizard');
    expect(r.status).toBe(201);
    const ch = r.body.character;
    expect(ch).toMatchObject({ name: 'Aria', classId: 'wizard', level: 1, rev: 0, online: false });
    expect(ch.state).toMatchObject({
      version: 1, classId: 'wizard', mapId: 'frostHollow', level: 1, exp: 0, gold: 0, hpPotion: 0, mpPotion: 0, upgradeStone: 0,
      hp: 0, mp: 0, x: 0, y: 0, hasPosition: false, bossDefeated: false, inventory: [], equipment: [],
    });
  });

  it('allows one character per class and max 5 per account', async () => {
    expect((await create(a.accessToken, 'Dup', 'wizard')).body.error.code).toBe('class_taken');
    for (const [i, cls] of ['warrior', 'mechanic', 'cleric', 'assassin'].entries()) {
      expect((await create(a.accessToken, `Alice${i}`, cls)).status).toBe(201);
    }
    const list = await c.call('GET', '/characters', { token: a.accessToken });
    expect(list.body.characters).toHaveLength(5);
    expect(new Set(list.body.characters.map((x) => x.classId)).size).toBe(5);
    expect((await create(a.accessToken, 'Sixth', 'warrior')).status).toBe(409); // no 6th: every class is taken
  });

  it('names are unique across accounts, case-insensitive', async () => {
    const r = await create(b.accessToken, 'ARIA', 'wizard');
    expect(r.status).toBe(409);
    expect(r.body.error.code).toBe('name_taken');
  });

  it('validates name and class and requires auth', async () => {
    expect((await create(b.accessToken, 'x', 'warrior')).status).toBe(400);
    expect((await create(b.accessToken, 'bad name', 'warrior')).status).toBe(400);
    expect((await create(b.accessToken, 'Fine_Name', 'paladin')).status).toBe(400);
    expect((await c.call('POST', '/characters', { body: { name: 'NoAuth', classId: 'warrior' } })).status).toBe(401);
  });

  it('lists only my characters and loads/deletes only my own', async () => {
    expect((await c.call('GET', '/characters', { token: b.accessToken })).body.characters).toHaveLength(0);
    const ch = (await create(b.accessToken, 'Bobchar', 'cleric')).body.character;
    const aliceId = (await c.call('GET', '/characters', { token: a.accessToken })).body.characters[0].id;
    expect((await c.call('GET', `/characters/${aliceId}`, { token: b.accessToken })).status).toBe(404);
    expect((await c.call('DELETE', `/characters/${aliceId}`, { token: b.accessToken })).status).toBe(404);
    expect((await c.call('GET', '/characters/not-an-id', { token: b.accessToken })).status).toBe(404);
    const loaded = await c.call('GET', `/characters/${ch.id}`, { token: b.accessToken });
    expect(loaded.body.character.state.classId).toBe('cleric');
    expect((await c.call('DELETE', `/characters/${ch.id}`, { token: b.accessToken })).status).toBe(200);
    expect((await c.call('GET', `/characters/${ch.id}`, { token: b.accessToken })).status).toBe(404);
    expect((await create(b.accessToken, 'Bobchar', 'cleric')).status).toBe(201); // the class slot is free again
  });

  it('cannot delete an online character', async () => {
    const o = await c.online('mechanic');
    const r = await c.call('DELETE', `/characters/${o.char.id}`, { token: o.user.accessToken });
    expect(r.status).toBe(409);
    expect(r.body.error.code).toBe('character_online');
  });
});
