import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { DEFAULT_MAX_CHARACTERS } from '../src/rpg/rpg-constants.js';
import { makeCtx } from './helpers.js';

// The character select screen (19/11/2026): one account holds up to N heroes, lists them in creation order, and only its owner may load, enter or delete one.
const create = (c, token, name, classId) => c.call('POST', '/characters', { token, body: { name, classId } });
const list = async (c, token) => (await c.call('GET', '/characters', { token })).body.characters;

describe('character roster: several heroes per account', () => {
  let c, owner, other;
  beforeAll(async () => { c = await makeCtx(); owner = await c.register('rosterowner'); other = await c.register('rosterother'); });
  afterAll(() => c.close());

  it('the default cap is 6', () => { expect(DEFAULT_MAX_CHARACTERS).toBe(6); });

  it('creates N heroes of mixed and repeated classes, listed oldest first, then refuses the next', async () => {
    const classes = ['warrior', 'warrior', 'wizard', 'cleric', 'assassin', 'archer']; // archer (2026-11-26): the new class id is accepted
    for (let i = 0; i < classes.length; i++) {
      c.clock.t += 1000; // distinct createdAt: the list is sorted by it
      expect((await create(c, owner.accessToken, `Roster${i}`, classes[i])).status).toBe(201);
    }
    const heroes = await list(c, owner.accessToken);
    expect(heroes.map((h) => h.name)).toEqual(classes.map((_, i) => `Roster${i}`));
    expect(heroes.map((h) => h.classId)).toEqual(classes);
    for (const h of heroes) expect(h).toMatchObject({ level: 1, online: false });
    const over = await create(c, owner.accessToken, 'RosterOver', 'mechanic');
    expect(over.status).toBe(409);
    expect(over.body.error.code).toBe('slots_full');
    expect(await list(c, owner.accessToken)).toHaveLength(6);
  });

  it('a delete frees a slot for a new hero', async () => {
    const [first] = await list(c, owner.accessToken);
    expect((await c.call('DELETE', `/characters/${first.id}`, { token: owner.accessToken })).status).toBe(200);
    expect((await c.call('GET', `/characters/${first.id}`, { token: owner.accessToken })).status).toBe(404);
    expect(await list(c, owner.accessToken)).toHaveLength(5);
    expect((await create(c, owner.accessToken, 'RosterNew', 'mechanic')).status).toBe(201);
    expect((await create(c, owner.accessToken, 'RosterNew2', 'mechanic')).status).toBe(409);
  });

  it('another account can neither see, load, enter nor delete them (404, not 403: no hint they exist)', async () => {
    const heroes = await list(c, owner.accessToken);
    expect(await list(c, other.accessToken)).toHaveLength(0);
    for (const h of heroes.slice(0, 2)) {
      expect((await c.call('GET', `/characters/${h.id}`, { token: other.accessToken })).status).toBe(404);
      expect((await c.call('POST', `/characters/${h.id}/enter`, { token: other.accessToken })).status).toBe(404);
      expect((await c.call('DELETE', `/characters/${h.id}`, { token: other.accessToken })).status).toBe(404);
    }
    expect(await list(c, owner.accessToken)).toHaveLength(heroes.length); // nothing was deleted
    expect((await c.call('DELETE', `/characters/${heroes[0].id}`)).status).toBe(401);
  });

  it('the cap is per account: the other account still has all its slots', async () => {
    for (let i = 0; i < 6; i++) expect((await create(c, other.accessToken, `Other${i}`, 'wizard')).status).toBe(201);
    expect((await create(c, other.accessToken, 'Other6', 'wizard')).status).toBe(409);
  });

  it('the owner enters any of its heroes; two of them can hold a ticket each', async () => {
    const heroes = await list(c, owner.accessToken);
    const a = await c.call('POST', `/characters/${heroes[0].id}/enter`, { token: owner.accessToken });
    const b = await c.call('POST', `/characters/${heroes[1].id}/enter`, { token: owner.accessToken });
    expect(a.status).toBe(200);
    expect(b.status).toBe(200);
    expect(a.body.ticket).not.toBe(b.body.ticket);
  });
});

describe('character roster: MAX_CHARACTERS overrides the cap', () => {
  let c, u;
  beforeAll(async () => { c = await makeCtx({ cfg: { MAX_CHARACTERS: '2' } }); u = await c.register('capped'); });
  afterAll(() => c.close());

  it('stops at the configured number', async () => {
    expect((await create(c, u.accessToken, 'CapA', 'warrior')).status).toBe(201);
    expect((await create(c, u.accessToken, 'CapB', 'cleric')).status).toBe(201);
    const r = await create(c, u.accessToken, 'CapC', 'wizard');
    expect(r.status).toBe(409);
    expect(r.body.error.code).toBe('slots_full');
  });
});

describe('character roster: slots on the character select screen', () => {
  let c, u;
  beforeAll(async () => { c = await makeCtx(); u = await c.register('slotter'); });
  afterAll(() => c.close());
  const make = (name, classId, slot) => c.call('POST', '/characters', { token: u.accessToken, body: { name, classId, ...(slot === undefined ? {} : { slot }) } });

  it('a new hero goes into the empty slot the player picked; the list is in slot order', async () => {
    expect((await make('SlotFour', 'wizard', 4)).body.character.slot).toBe(4);
    expect((await make('SlotOne', 'cleric', 1)).body.character.slot).toBe(1);
    expect((await list(c, u.accessToken)).map((h) => [h.name, h.slot])).toEqual([['SlotOne', 1], ['SlotFour', 4]]);
  });

  it('no slot given: the first free one', async () => {
    expect((await make('SlotAuto', 'warrior')).body.character.slot).toBe(0);
    expect((await make('SlotAuto2', 'warrior')).body.character.slot).toBe(2);
  });

  it('a taken slot or one past the cap is refused and nothing is created', async () => {
    const taken = await make('SlotDup', 'mechanic', 4);
    expect(taken.status).toBe(409);
    expect(taken.body.error.code).toBe('slot_taken');
    expect((await make('SlotFar', 'mechanic', 6)).body.error.code).toBe('slot_taken');
    expect((await make('SlotNeg', 'mechanic', -1)).status).toBe(400);
    expect((await make('SlotStr', 'mechanic', 'x')).status).toBe(400);
    expect(await list(c, u.accessToken)).toHaveLength(4);
  });

  it('a deleted hero frees its slot for the next one', async () => {
    const four = (await list(c, u.accessToken)).find((h) => h.slot === 4);
    expect((await c.call('DELETE', `/characters/${four.id}`, { token: u.accessToken })).status).toBe(200);
    expect((await make('SlotAgain', 'assassin', 4)).body.character.slot).toBe(4);
  });

  it('heroes stored before slots existed get the free slots in creation order', async () => {
    const v = await c.register('oldslots');
    for (const n of ['OldA', 'OldB']) {
      c.clock.t += 1000;
      await c.call('POST', '/characters', { token: v.accessToken, body: { name: n, classId: 'warrior', slot: n === 'OldA' ? 3 : 5 } });
    }
    await c.db.collection('characters').updateMany({ nameLower: { $in: ['olda', 'oldb'] } }, { $unset: { slot: '' } }); // as an older server stored them
    const made = await c.call('POST', '/characters', { token: v.accessToken, body: { name: 'NewC', classId: 'cleric' } });
    expect(made.body.character.slot).toBe(2);
    expect((await list(c, v.accessToken)).map((h) => [h.name, h.slot])).toEqual([['OldA', 0], ['OldB', 1], ['NewC', 2]]);
  });
});
