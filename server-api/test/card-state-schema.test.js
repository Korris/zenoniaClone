import { afterAll, beforeAll, describe, expect, it } from 'vitest';
import { newCharacterState, stateSchema } from '../src/rpg/item-validation.js';
import { ALBUM_MILESTONES, CARD_COUNT, CARD_IDS, GEM_MAX_ROWS } from '../src/rpg/rpg-constants.js';
import { makeCtx, weapon } from './helpers.js';

// Monster cards (save v6): every new SaveData field must survive the zod strip, in the schema and through the save API.
const base = () => newCharacterState('warrior');
const cards = [{ id: 'meadowSlime', n: 3, locked: 1 }, { id: 'frostfangKing', n: 1, locked: 0 }];
const full = () => ({
  cards,
  gems: [{ id: 'snowFox', kind: 1, locked: true, n: 2 }, { id: 'meadowSlime', kind: 0, locked: false, n: 1 }],
  album: [0, 20, 5],
  albumClaimed: 1,
  cardDeck: ['meadowSlime', 'meadowSlime', 'frostfangKing'],
  cardDaily: { day: 20000, npcWins: 2, pvpRewards: 3, wagers: 1 },
  cardNpcFirstWins: 5,
  cardRank: { season: 2, mmr: 1500, lp: 40, wins: 7, losses: 3, draws: 1, best: 1610, prevBest: 820, rewards: 6 },
  cardHelp: 1,
});

describe('card ids', () => {
  it('mirror the C# table: 53 unique ids (40 pre-Taurus + 12 Taurus + the Matriarch field boss card, appended), old bosses SSR, the Taurus boss SR', async () => {
    expect(CARD_COUNT).toBe(53);
    expect(new Set(CARD_IDS).size).toBe(53);
    expect(CARD_IDS[0]).toBe('meadowSlime');
    expect(CARD_IDS.slice(40, 46)).toEqual(['tauCloverSlime', 'tauHornRabbit', 'tauReedFrog', 'tauMossMushroom', 'tauBrambleBoar', 'tauLeafMantis']); // Orin's starter cards + Boar; append-only indexes
    const table = (await import('../src/rpg/card-ids.json', { with: { type: 'json' } })).default;
    expect(table.filter((c) => c.isBoss).map((c) => c.id).sort()).toEqual(['ambercapWarden', 'frostfangKing', 'mirefangHydra', 'rimeclawTyrant', 'tauBramblebackMatriarch', 'tauElderRootGuardian', 'thornfangAlpha']);
    for (const c of table.slice(0, 40)) expect(c.isBoss).toBe(c.rarity === 'SSR');
    expect(table.find((c) => c.id === 'tauElderRootGuardian').rarity).toBe('SR');
    expect(table[52]).toEqual({ id: 'tauBramblebackMatriarch', rarity: 'R', isBoss: true });
  });
});

describe('state schema: card fields', () => {
  it('keeps every card field', () => {
    const s = stateSchema.parse({ ...base(), version: 6, ...full() });
    expect(s).toMatchObject(full());
  });

  it('cardHelp (first-time hint flag) defaults to 0 and refuses values beyond the known bits', () => {
    expect(stateSchema.parse({ ...base(), version: 5 }).cardHelp).toBe(0);
    expect(stateSchema.parse({ ...base(), version: 6, cardHelp: 1 }).cardHelp).toBe(1);
    expect(stateSchema.parse({ ...base(), version: 6, cardHelp: 2 }).cardHelp).toBe(2); // bit 1 = first PvP risk box confirmed
    expect(stateSchema.parse({ ...base(), version: 6, cardHelp: 3 }).cardHelp).toBe(3);
    expect(stateSchema.parse({ ...base(), version: 7, cardHelp: 7 }).cardHelp).toBe(7); // bit 2 = Orin Guidance accepted
    expect(() => stateSchema.parse({ ...base(), version: 6, cardHelp: 8 })).toThrow();
  });

  it('keeps the first-card-duel quest in every state (free-form quest id)', () => {
    for (const state of [1, 2, 3]) {
      const s = stateSchema.parse({ ...base(), version: 6, quests: [{ id: 'first-card-duel', state, progress: state === 1 ? 0 : 1 }] });
      expect(s.quests).toEqual([{ id: 'first-card-duel', state, progress: state === 1 ? 0 : 1 }]);
    }
  });

  it('defaults an old save to nothing owned', () => {
    const s = stateSchema.parse({ ...base(), version: 5 });
    expect(s).toMatchObject({ cards: [], gems: [], album: [], albumClaimed: 0, cardDeck: [], cardNpcFirstWins: 0 });
    expect(s.cardDaily).toEqual({ day: 0, npcWins: 0, pvpRewards: 0, wagers: 0 });
    expect(s.cardRank).toEqual({ season: 0, mmr: 0, lp: 0, wins: 0, losses: 0, draws: 0, best: 0, prevBest: 0, rewards: 0 });
  });

  it('an older cardRank without the season-end fields gets them as 0', () => {
    const s = stateSchema.parse({ ...base(), version: 6, cardRank: { season: 1, mmr: 1100, lp: 50 } });
    expect(s.cardRank).toEqual({ season: 1, mmr: 1100, lp: 50, wins: 0, losses: 0, draws: 0, best: 0, prevBest: 0, rewards: 0 });
  });

  it('a card row without locked (older v6 save) means none locked', () => {
    const s = stateSchema.parse({ ...base(), version: 6, cards: [{ id: 'meadowSlime', n: 3 }] });
    expect(s.cards).toEqual([{ id: 'meadowSlime', n: 3, locked: 0 }]);
  });

  it('accepts save version 7 and rejects 8', () => {
    expect(stateSchema.safeParse({ ...base(), version: 7 }).success).toBe(true);
    expect(stateSchema.safeParse({ ...base(), version: 8 }).success).toBe(false);
  });

  it.each([
    ['an unknown card', { cards: [{ id: 'ghost', n: 1 }] }],
    ['a zero stack', { cards: [{ id: 'meadowSlime', n: 0 }] }],
    ['a lock value above 1 (the lock is a flag per card kind)', { cards: [{ id: 'meadowSlime', n: 2, locked: 2 }] }],
    ['a negative lock', { cards: [{ id: 'meadowSlime', n: 2, locked: -1 }] }],
    ['a stack over 999', { cards: [{ id: 'meadowSlime', n: 1000 }] }],
    ['more than 53 card rows', { cards: Array.from({ length: 54 }, () => ({ id: 'meadowSlime', n: 1 })) }],
    ['an unknown gem card', { gems: [{ id: 'ghost', kind: 0, locked: false, n: 1 }] }],
    ['a gem kind 2', { gems: [{ id: 'snowFox', kind: 2, locked: false, n: 1 }] }],
    ['more than the gem row limit', { gems: Array.from({ length: GEM_MAX_ROWS + 1 }, () => ({ id: 'snowFox', kind: 0, locked: false, n: 1 })) }],
    ['an album index of 53', { album: [53] }],
    ['an album claim bit past the 4 milestones', { albumClaimed: 1 << ALBUM_MILESTONES }],
    ['a 9-card deck', { cardDeck: Array.from({ length: 9 }, () => 'meadowSlime') }],
    ['an unknown deck card', { cardDeck: ['ghost'] }],
    ['a negative daily counter', { cardDaily: { day: 1, npcWins: -1 } }],
    ['a negative rank value', { cardRank: { mmr: -5 } }],
    ['season rewards past the 5 tier bits', { cardRank: { rewards: 32 } }],
    ['a negative previous-season best', { cardRank: { prevBest: -1 } }],
    ['an NPC bit field over 16 bits', { cardNpcFirstWins: 0x10000 }],
  ])('rejects %s', (_name, patch) => {
    expect(stateSchema.safeParse({ ...base(), version: 6, ...patch }).success).toBe(false);
  });
});

describe('item sockets', () => {
  const item = (sockets) => ({ ...weapon('w-1'), ...(sockets === undefined ? {} : { sockets }) });

  it('keeps socketed gems and defaults to none', () => {
    const s = stateSchema.parse({ ...base(), version: 6, inventory: [item(['meadowSlime:p', 'frostBear:g']), item(undefined)].map((i, n) => ({ ...i, uid: `w-${n}` })) });
    expect(s.inventory[0].sockets).toEqual(['meadowSlime:p', 'frostBear:g']);
    expect(s.inventory[1].sockets).toEqual([]);
  });

  it.each([
    ['three gems', ['meadowSlime:p', 'frostBear:g', 'thornBoar:p']],
    ['an unknown card', ['ghost:p']],
    ['a bad kind', ['meadowSlime:x']],
    ['no kind', ['meadowSlime']],
    ['a shell-ish string', ['meadowSlime:p;rm']],
  ])('rejects %s', (_name, sockets) => {
    expect(stateSchema.safeParse({ ...base(), version: 6, inventory: [item(sockets)] }).success).toBe(false);
  });
});

describe('save API round trip', () => {
  let c;
  beforeAll(async () => { c = await makeCtx(); });
  afterAll(() => c.close());

  it('keeps cards, gems, album, deck, counters, rank and sockets after save and load', async () => {
    const o = await c.online();
    const state = { ...o.state, version: 6, ...full(), inventory: [{ ...weapon('w-1'), sockets: ['meadowSlime:p', 'snowFox:g'] }] };
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state });
    expect(r.status).toBe(200);
    const loaded = (await c.call('GET', `/characters/${o.char.id}`, { token: o.user.accessToken })).body.character;
    expect(loaded.state).toMatchObject(full());
    expect(loaded.state.inventory[0].sockets).toEqual(['meadowSlime:p', 'snowFox:g']);
  });

  it('refuses a save carrying an unknown card', async () => {
    const o = await c.online();
    const r = await c.svc('POST', `/internal/characters/${o.char.id}/save`, { sessionId: o.sessionId, rev: o.rev, state: { ...o.state, cards: [{ id: 'ghost', n: 1 }] } });
    expect(r.status).toBe(400);
  });
});
