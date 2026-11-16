import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { makeCtx } from './helpers.js';

let c;
beforeAll(async () => { c = await makeCtx(); });
afterAll(() => c.close());

const accGet = (o) => c.svc('POST', `/internal/characters/${o.char.id}/account/get`, { sessionId: o.sessionId });
const accWrite = (o, state, rev, reqId) => c.svc('POST', `/internal/characters/${o.char.id}/account/write`, { sessionId: o.sessionId, rev, reqId, state });

describe('internal account routes', () => {
  it('redeem creates a default account on first login', async () => {
    const o = await c.online();
    expect(o.account.rev).toBe(0);
    expect(o.account.state).toMatchObject({ version: 2, pets: [], tickets: 0, starterGiven: false, totalPulls: 0 });
  });

  it('write then get round-trips the V2 state and bumps rev', async () => {
    const o = await c.online();
    const state = { ...o.account.state, tickets: 5, totalPulls: 1, pets: [{ id: 'spirit', stars: 1, shards: 3, level: 4 }] };
    const w = await accWrite(o, state, 0, 'req-acc-roundtrip');
    expect(w.status).toBe(200);
    expect(w.body).toMatchObject({ rev: 1, replayed: false });
    const g = await accGet(o);
    expect(g.status).toBe(200);
    expect(g.body.rev).toBe(1);
    expect(g.body.state).toMatchObject({ tickets: 5, totalPulls: 1, pets: [{ id: 'spirit', stars: 1, shards: 3, level: 4 }] });
  });

  it('replays a seen reqId without bumping rev twice', async () => {
    const o = await c.online();
    const state = { ...o.account.state, tickets: 2 };
    const first = await accWrite(o, state, 0, 'req-acc-replay');
    expect(first.body).toMatchObject({ rev: 1, replayed: false });
    const replay = await accWrite(o, state, 0, 'req-acc-replay');
    expect(replay.body).toMatchObject({ rev: 1, replayed: true });
    expect((await accGet(o)).body.rev).toBe(1);
  });

  it('rejects a stale rev with 409 rev_conflict + currentRev', async () => {
    const o = await c.online();
    expect((await accWrite(o, { ...o.account.state, tickets: 1 }, 0, 'req-acc-rev1')).status).toBe(200);
    const stale = await accWrite(o, { ...o.account.state, tickets: 2 }, 0, 'req-acc-rev2');
    expect(stale.status).toBe(409);
    expect(stale.body.error).toMatchObject({ code: 'rev_conflict', details: { currentRev: 1 } });
  });

  it('422 account_tamper: a pet goes missing or loses stars/level', async () => {
    const o = await c.online();
    const withPet = { ...o.account.state, pets: [{ id: 'spirit', stars: 2, level: 10 }] };
    expect((await accWrite(o, withPet, 0, 'req-tamper-pet1')).status).toBe(200);
    const missing = await accWrite(o, { ...withPet, pets: [] }, 1, 'req-tamper-pet2');
    expect(missing.status).toBe(422);
    expect(missing.body.error.code).toBe('account_tamper');
    const lowered = await accWrite(o, { ...withPet, pets: [{ id: 'spirit', stars: 1, level: 10 }] }, 1, 'req-tamper-pet3');
    expect(lowered.status).toBe(422);
    expect(lowered.body.error.code).toBe('account_tamper');
    const leveledDown = await accWrite(o, { ...withPet, pets: [{ id: 'spirit', stars: 2, level: 5 }] }, 1, 'req-tamper-pet4');
    expect(leveledDown.status).toBe(422);
    expect(leveledDown.body.error.code).toBe('account_tamper');
  });

  it('422 account_tamper: compensated shrinks', async () => {
    const o = await c.online();
    const withComp = { ...o.account.state, compensated: ['spirit'] };
    expect((await accWrite(o, withComp, 0, 'req-tamper-comp1')).status).toBe(200);
    const shrink = await accWrite(o, { ...withComp, compensated: [] }, 1, 'req-tamper-comp2');
    expect(shrink.status).toBe(422);
    expect(shrink.body.error.code).toBe('account_tamper');
  });

  it('422 account_tamper: starterGiven reverts to false', async () => {
    const o = await c.online();
    expect((await accWrite(o, { ...o.account.state, starterGiven: true }, 0, 'req-tamper-starter1')).status).toBe(200);
    const revert = await accWrite(o, { ...o.account.state, starterGiven: false }, 1, 'req-tamper-starter2');
    expect(revert.status).toBe(422);
    expect(revert.body.error.code).toBe('account_tamper');
  });

  it('422 account_tamper: totalPulls decreases', async () => {
    const o = await c.online();
    expect((await accWrite(o, { ...o.account.state, totalPulls: 5 }, 0, 'req-tamper-pulls1')).status).toBe(200);
    const lower = await accWrite(o, { ...o.account.state, totalPulls: 3 }, 1, 'req-tamper-pulls2');
    expect(lower.status).toBe(422);
    expect(lower.body.error.code).toBe('account_tamper');
  });

  it('422 account_tamper: levelReward decreases', async () => {
    const o = await c.online();
    expect((await accWrite(o, { ...o.account.state, levelReward: 10 }, 0, 'req-tamper-lvl1')).status).toBe(200);
    const lower = await accWrite(o, { ...o.account.state, levelReward: 5 }, 1, 'req-tamper-lvl2');
    expect(lower.status).toBe(422);
    expect(lower.body.error.code).toBe('account_tamper');
  });

  it('wantedForRareBall may drop (resets to 0 once a rare ball is granted): not tamper', async () => {
    const o = await c.online();
    expect((await accWrite(o, { ...o.account.state, wantedForRareBall: 27 }, 0, 'req-wanted1')).status).toBe(200);
    const dropped = await accWrite(o, { ...o.account.state, wantedForRareBall: 0, rareBalls: 1 }, 1, 'req-wanted2');
    expect(dropped.status).toBe(200);
  });

  it('a stored version-1 account doc migrates on read (weekWantedTickets -> weekWantedClaims, pity clamped)', async () => {
    const o = await c.online();
    await c.db.collection('accounts').updateOne(
      { _id: o.user.user.id },
      { $set: { state: { version: 1, pets: [], tickets: 1, pitySR: 2, pitySSR: 25, pityUR: 55, weekWantedTickets: 3, history: [], compensated: [] } } },
    );
    const g = await accGet(o);
    expect(g.status).toBe(200);
    expect(g.body.state).toMatchObject({ version: 2, tickets: 1, pitySSR: 19, pityUR: 49, weekWantedClaims: 3 });
    expect(g.body.state.pitySR).toBeUndefined();
    expect(g.body.state.weekWantedTickets).toBeUndefined();
  });
});

describe('character state v5 pet team', () => {
  it('keeps petTeam at version 5', async () => {
    const o = await c.online();
    const state = { ...o.state, version: 5, petTeam: ['spirit', '', 'frostSlime', '', ''] };
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state });
    expect(r.status).toBe(200);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.state).toMatchObject({ version: 5, petTeam: ['spirit', '', 'frostSlime', '', ''] });
  });

  it('rejects an unknown pet id, duplicate ids, or more than 5 slots in petTeam', async () => {
    const o = await c.online();
    const save = (state) => c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state });
    expect((await save({ ...o.state, version: 5, petTeam: ['dragon'] })).status).toBe(400);
    expect((await save({ ...o.state, version: 5, petTeam: ['spirit', 'spirit'] })).status).toBe(400);
    expect((await save({ ...o.state, version: 5, petTeam: ['spirit', 'imp', 'frostSlime', 'hornRabbit', 'iceGoblin', 'thornBoar'] })).status).toBe(400);
  });
});

describe('character state v4 companion field', () => {
  it('keeps petId at version 4', async () => {
    const o = await c.online();
    const state = { ...o.state, version: 4, petId: 'spirit' };
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state });
    expect(r.status).toBe(200);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.state).toMatchObject({ version: 4, petId: 'spirit' });
  });

  it('rejects an unknown petId', async () => {
    const o = await c.online();
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state: { ...o.state, petId: 'dragon' } });
    expect(r.status).toBe(400);
  });
});
