import { describe, expect, it } from 'vitest';
import { accountStateSchema, migrateAccountState } from '../src/rpg/account-validation.js';

describe('accountStateSchema (V2)', () => {
  it('parses {} to the default account state', () => {
    const s = accountStateSchema.parse({});
    expect(s).toMatchObject({
      version: 2, pets: [], tickets: 0, crystals: 0, urShards: 0, sharedShards: 0, food: 0, balls: 0, rareBalls: 0,
      pitySSR: 0, pityUR: 0, totalPulls: 0, history: [], starterGiven: false, compensated: [], levelReward: 0,
      wantedForRareBall: 0, ballsBoughtToday: 0,
      rewardDay: 0, dayWanted: false, dayBossCaught: false, dayWantedCaught: false,
      rewardWeek: 0, weekWantedClaims: 0, weekBossKills: 0,
    });
  });

  it('rejects an unknown pet id and pity out of range', () => {
    expect(accountStateSchema.safeParse({ pets: [{ id: 'dragon', stars: 1 }] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ compensated: ['dragon'] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ history: [{ id: 'not-a-pet' }] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ pitySSR: 19 }).success).toBe(true);
    expect(accountStateSchema.safeParse({ pitySSR: 20 }).success).toBe(false);
    expect(accountStateSchema.safeParse({ pityUR: 49 }).success).toBe(true);
    expect(accountStateSchema.safeParse({ pityUR: 50 }).success).toBe(false);
  });

  it('rejects duplicate pet ids in pets / compensated', () => {
    expect(accountStateSchema.safeParse({ pets: [{ id: 'spirit', stars: 1 }, { id: 'spirit', stars: 2 }] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ compensated: ['spirit', 'spirit'] }).success).toBe(false);
  });

  it('keeps per-pet level, shards and spent ledgers', () => {
    const s = accountStateSchema.parse({ pets: [{ id: 'crystalGolem', stars: 2, shards: 7, level: 15, foodSpent: 3, shardsSpent: 20, goldSpent: 15000 }] });
    expect(s.pets[0]).toMatchObject({ id: 'crystalGolem', stars: 2, shards: 7, level: 15, foodSpent: 3, shardsSpent: 20, goldSpent: 15000 });
  });

  it('rejects a pet level or stars out of range', () => {
    expect(accountStateSchema.safeParse({ pets: [{ id: 'spirit', stars: 6 }] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ pets: [{ id: 'spirit', stars: 1, level: 61 }] }).success).toBe(false);
    expect(accountStateSchema.safeParse({ pets: [{ id: 'spirit', stars: 1, level: 0 }] }).success).toBe(false);
  });

  it('a history entry with id "" (shard result) is accepted', () => {
    const s = accountStateSchema.parse({ history: [{ at: 1, id: '', dup: false, shards: 5, pitySSR: 1, pityUR: 2 }] });
    expect(s.history[0]).toMatchObject({ id: '', shards: 5 });
  });
});

describe('migrateAccountState', () => {
  it('maps weekWantedTickets to weekWantedClaims and clamps grown-then-shrunk pity, then parses as V2', () => {
    const oldDoc = { version: 1, pitySR: 5, pitySSR: 25, pityUR: 55, weekWantedTickets: 3, history: [{ at: 1, id: '', pitySSR: 29, pityUR: 59 }] };
    const migrated = migrateAccountState(oldDoc);
    expect(migrated.weekWantedClaims).toBe(3);
    const s = accountStateSchema.parse(migrated);
    expect(s).toMatchObject({ version: 2, pitySSR: 19, pityUR: 49, weekWantedClaims: 3 });
    expect(s.history[0]).toMatchObject({ pitySSR: 19, pityUR: 49 });
    expect(s.pitySR).toBeUndefined();
  });

  it('is a no-op on an already-V2 document', () => {
    const v2 = accountStateSchema.parse({ pitySSR: 10, pityUR: 20, weekWantedClaims: 2 });
    expect(accountStateSchema.parse(migrateAccountState(v2))).toMatchObject(v2);
  });
});
