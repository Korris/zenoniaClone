import { z } from 'zod';
import { ACCOUNT_HISTORY_MAX, MAX_INT, PET_IDS, PET_MAX_LEVEL, PET_MAX_STARS } from './rpg-constants.js';

// Mirrors Assets/Scripts/Rpg/Account/AccountState.cs (JsonUtility, OwnedPet/PullRecord/AccountState, version 2 = PetRules).
// Field names match 1:1 so the client reads this as-is. Account-level data (owned pets, gacha pulls, pity, capture balls,
// daily/weekly rewards); a character only stores its team (petTeam, item-validation.js).

const MAX_PETS = 16; // AccountState.MaxPets
const SSR_PITY_MAX = 19; // PetRules.SsrPity - 1
const UR_PITY_MAX = 49; // PetRules.UrPity - 1
const MILESTONE_MAX = 90; // PetRules.MilestoneMax
const WANTED_FOR_RARE_BALL_MAX = 27; // PetRules.WantedClaimsForRareBall - 1
const BALLS_PER_DAY = 5; // PetRules.BallsPerDay
const WANTED_PER_WEEK = 4; // PetRules.WantedPerWeek

const int0 = z.number().int().min(0).max(MAX_INT);
const petId = z.enum(PET_IDS);
const pitySSR = z.number().int().min(0).max(SSR_PITY_MAX);
const pityUR = z.number().int().min(0).max(UR_PITY_MAX);

const petSchema = z.object({
  id: petId,
  stars: z.number().int().min(1).max(PET_MAX_STARS),
  shards: int0.default(0), // spare copies of an already-owned pet, banked towards its next star
  level: z.number().int().min(1).max(PET_MAX_LEVEL).default(1),
  foodSpent: int0.default(0), shardsSpent: int0.default(0), goldSpent: int0.default(0), // spent on this pet's level/stars, so a reset refunds exactly that
});

const historyEntrySchema = z.object({
  at: z.number().int().min(0).default(0),
  id: z.union([petId, z.literal('')]).default(''), // "" = the pull gave shared/kind shards, not a pet
  dup: z.boolean().default(false),
  shards: z.number().int().min(0).max(1000).default(0),
  pitySSR: pitySSR.default(0), pityUR: pityUR.default(0),
});

/** AccountState: the whole persisted account state (owned pets, gacha pulls, pity, capture balls, daily/weekly rewards). Unknown keys stripped. */
export const accountStateSchema = z
  .object({
    version: z.number().int().min(0).max(2).default(2),
    pets: z.array(petSchema).max(MAX_PETS).default([]),
    tickets: int0.default(0), crystals: int0.default(0), urShards: int0.default(0), sharedShards: int0.default(0),
    food: int0.default(0), balls: int0.default(0), rareBalls: int0.default(0),
    pitySSR: pitySSR.default(0), pityUR: pityUR.default(0),
    totalPulls: int0.default(0),
    history: z.array(historyEntrySchema).max(ACCOUNT_HISTORY_MAX).default([]),
    starterGiven: z.boolean().default(false),
    compensated: z.array(petId).max(MAX_PETS).default([]),
    levelReward: z.number().int().min(0).max(MILESTONE_MAX).default(0),
    wantedForRareBall: z.number().int().min(0).max(WANTED_FOR_RARE_BALL_MAX).default(0),
    ballsBoughtToday: z.number().int().min(0).max(BALLS_PER_DAY).default(0),
    rewardDay: z.number().int().min(0).max(1_000_000).default(0),
    dayWanted: z.boolean().default(false), dayBossCaught: z.boolean().default(false), dayWantedCaught: z.boolean().default(false),
    rewardWeek: z.number().int().min(0).max(1_000_000).default(0),
    weekWantedClaims: z.number().int().min(0).max(WANTED_PER_WEEK).default(0),
    weekBossKills: z.number().int().min(0).max(999).default(0),
  })
  .superRefine((s, ctx) => {
    const dup = (arr, path, label) => {
      const seen = new Set();
      for (const id of arr) {
        if (seen.has(id)) ctx.addIssue({ code: 'custom', path: [path], message: `duplicate ${label} ${id}` });
        seen.add(id);
      }
    };
    dup(s.pets.map((p) => p.id), 'pets', 'pet id');
    dup(s.compensated, 'compensated', 'pet id');
  });

/** Brand-new account: no pets/pulls yet, nothing claimed today or this week. */
export function newAccountState() {
  return accountStateSchema.parse({});
}

/**
 * Normalizes an OLD stored account doc (version 0/1: pity counters were 10/30/60-based and the weekly
 * Wanted counter was `weekWantedTickets`) to the V2 shape before `accountStateSchema.parse`: maps
 * weekWantedTickets -> weekWantedClaims and clamps the pity counters, whose cap shrank (30/60 -> 20/50),
 * so an old document parses instead of being rejected. A no-op on an already-V2 document.
 */
export function migrateAccountState(raw) {
  if (!raw || typeof raw !== 'object') return raw;
  const clamp = (v, max) => (typeof v === 'number' ? Math.min(Math.max(v, 0), max) : v);
  const s = { ...raw };
  if (s.weekWantedClaims == null && s.weekWantedTickets != null) s.weekWantedClaims = s.weekWantedTickets;
  s.pitySSR = clamp(s.pitySSR, SSR_PITY_MAX);
  s.pityUR = clamp(s.pityUR, UR_PITY_MAX);
  if (Array.isArray(s.history)) {
    s.history = s.history.map((h) => (h && typeof h === 'object' ? { ...h, pitySSR: clamp(h.pitySSR, SSR_PITY_MAX), pityUR: clamp(h.pityUR, UR_PITY_MAX) } : h));
  }
  s.version = 2;
  return s;
}

/**
 * Anti-tamper for account snapshots written by the zone server: nothing earned may be taken away.
 * Returns the offending reason string, or null if `newState` is a legal evolution of `oldState`.
 * wantedForRareBall is NOT guarded: it resets to 0 once a rare ball is granted (28 -> 0) by design.
 * dayWanted/dayBossCaught/dayWantedCaught/weekBossKills are NOT guarded: they reset daily/weekly.
 */
export function findAccountTamper(oldState, newState) {
  const oldPets = new Map(oldState.pets.map((p) => [p.id, p]));
  for (const [id, old] of oldPets) {
    const next = newState.pets.find((p) => p.id === id);
    if (!next) return `pet ${id} is missing`;
    if (next.stars < old.stars) return `pet ${id} stars went from ${old.stars} to ${next.stars}`;
    if (next.level < old.level) return `pet ${id} level went from ${old.level} to ${next.level}`;
  }
  for (const id of oldState.compensated) {
    if (!newState.compensated.includes(id)) return `compensated ${id} is missing`;
  }
  if (oldState.starterGiven && !newState.starterGiven) return 'starterGiven went from true to false';
  if (newState.totalPulls < oldState.totalPulls) return `totalPulls went from ${oldState.totalPulls} to ${newState.totalPulls}`;
  if (newState.levelReward < oldState.levelReward) return `levelReward went from ${oldState.levelReward} to ${newState.levelReward}`;
  return null;
}
