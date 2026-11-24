import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

// POST /characters with the look picked on the creation screen (docs/character-appearance-layered-spec.md, step E):
// stored as state.appearance, shown in the list, handed to the zone on redeem, kept through saves; ranges checked; optional for old clients.
let c, u;
beforeAll(async () => { c = await makeCtx(); u = await c.register('looker'); });
afterAll(() => c.close());

const create = (name, classId, appearance) => c.call('POST', '/characters', { token: u.accessToken, body: { name, classId, ...(appearance !== undefined ? { appearance } : {}) } });
const stored = (style, hair, outfit) => ({ stored: true, hairStyle: style, hairColorIndex: hair, outfitColorIndex: outfit, equippedHeadgearId: 0 });

describe('create with appearance', () => {
  it('stores the picked look (bare head) in the state', async () => {
    const r = await create('Lookie', 'assassin', { hairStyle: 0, hairColorIndex: 7, outfitColorIndex: 4 });
    expect(r.status).toBe(201);
    expect(r.body.character.state.appearance).toEqual(stored(0, 7, 4));
    expect(r.body.character.appearance).toEqual(stored(0, 7, 4));
  });

  it('defaults the hairstyle to 0 when only colours are sent', async () => {
    const r = await create('NoStyle', 'warrior', { hairColorIndex: 0, outfitColorIndex: 0 });
    expect(r.status).toBe(201);
    expect(r.body.character.state.appearance).toEqual(stored(0, 0, 0));
  });

  it('shows the look in the character list and in GET', async () => {
    const list = (await c.call('GET', '/characters', { token: u.accessToken })).body.characters;
    const hero = list.find((x) => x.name === 'Lookie');
    expect(hero.appearance).toEqual(stored(0, 7, 4));
    const read = await c.call('GET', `/characters/${hero.id}`, { token: u.accessToken });
    expect(read.body.character.state.appearance).toEqual(stored(0, 7, 4));
  });

  it('without appearance (older client) keeps no look: the name-hash default applies', async () => {
    const r = await create('OldClient', 'cleric');
    expect(r.status).toBe(201);
    expect(r.body.character.state.appearance).toBeUndefined();
    expect(r.body.character.appearance).toBeUndefined();
  });

  it.each([
    ['hair colour past the palette', { hairColorIndex: 9, outfitColorIndex: 0 }],
    ['outfit colour past the palette', { hairColorIndex: 0, outfitColorIndex: 6 }],
    ['hair colour not chosen', { hairColorIndex: -1, outfitColorIndex: 0 }],
    ['outfit colour missing', { hairColorIndex: 1 }],
    ['negative style', { hairStyle: -1, hairColorIndex: 0, outfitColorIndex: 0 }],
    ['style above one byte', { hairStyle: 256, hairColorIndex: 0, outfitColorIndex: 0 }],
    ['fractional colour', { hairColorIndex: 1.5, outfitColorIndex: 0 }],
    ['a hat at creation', { hairColorIndex: 0, outfitColorIndex: 0, equippedHeadgearId: 3 }],
    ['not an object', 'red'],
  ])('refuses %s (400, nothing created)', async (_n, bad) => {
    const before = (await c.call('GET', '/characters', { token: u.accessToken })).body.characters.length;
    const r = await create('BadLook', 'wizard', bad);
    expect(r.status).toBe(400);
    expect(r.body.error.code).toBe('validation_error');
    expect((await c.call('GET', '/characters', { token: u.accessToken })).body.characters.length).toBe(before);
  });

  it('reaches the zone on redeem and survives a save', async () => {
    const v = await c.register('zoner');
    const made = (await c.call('POST', '/characters', { token: v.accessToken, body: { name: 'ZoneLook', classId: 'mechanic', appearance: { hairColorIndex: 3, outfitColorIndex: 5 } } })).body.character;
    const e = await c.call('POST', `/characters/${made.id}/enter`, { token: v.accessToken });
    const r = await c.svc('POST', '/internal/tickets/redeem', { ticket: e.body.ticket });
    expect(r.status).toBe(200);
    expect(r.body.character.state.appearance).toEqual(stored(0, 3, 5));
    const state = { ...r.body.character.state, gold: 42 };
    const s = await c.svc('POST', `/internal/characters/${made.id}/save`, { sessionId: r.body.sessionId, rev: r.body.character.rev, state });
    expect(s.status).toBe(200);
    const read = await c.call('GET', `/characters/${made.id}`, { token: v.accessToken });
    expect(read.body.character.state.gold).toBe(42);
    expect(read.body.character.state.appearance).toEqual(stored(0, 3, 5));
  });
});
