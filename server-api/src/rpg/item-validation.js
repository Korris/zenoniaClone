import { z } from 'zod';
import { CLASS_IDS, INVENTORY_SIZE, MAX_ENHANCE, MAX_INT, MAX_LEVEL, MAX_STONES, RARITIES, SLOTS } from './rpg-constants.js';

// Field names match Assets/Scripts/Rpg/Item.cs (Item, ItemStats) and SaveSerializer.cs (SaveData) so Unity's JsonUtility reads them as-is.

const stat = z.number().int().min(0).max(999);
const frac = z.number().finite().min(0).max(999);

export const itemStatsSchema = z.object({
  atk: stat.default(0), def: stat.default(0), maxHp: stat.default(0), maxMp: stat.default(0),
  critChance: frac.default(0), speedMult: frac.default(0),
});

export const itemSchema = z.object({
  uid: z.string().min(1).max(64).regex(/^[A-Za-z0-9._-]+$/),
  templateId: z.string().min(1).max(64).regex(/^[A-Za-z0-9._-]+$/),
  slot: z.enum(SLOTS),
  name: z.string().min(1).max(40),
  rarity: z.enum(RARITIES),
  classId: z.union([z.enum(CLASS_IDS), z.literal('')]).optional().default(''), // C# JsonUtility writes '' for null
  itemLevel: z.number().int().min(1).max(999).default(1),
  enhance: z.number().int().min(0).max(MAX_ENHANCE).default(0),
  stats: itemStatsSchema.default({}),
});

const int0 = z.number().int().min(0).max(MAX_INT);
const f0 = z.number().finite().min(0).max(1e9);
const coord = z.number().finite().min(-1e6).max(1e6);

/** SaveData: the whole persisted character state. Unknown keys are stripped. */
export const stateSchema = z
  .object({
    version: z.number().int().min(0).max(1).default(1),
    savedAt: z.number().int().min(0).default(0),
    classId: z.enum(CLASS_IDS),
    mapId: z.string().min(1).max(32).regex(/^[A-Za-z0-9_-]+$/).default('frostHollow'),
    level: z.number().int().min(1).max(MAX_LEVEL),
    exp: int0, gold: int0, hpPotion: int0, mpPotion: int0,
    upgradeStone: z.number().int().min(0).max(MAX_STONES),
    hp: f0, mp: f0, x: coord, y: coord,
    hasPosition: z.boolean().default(false),
    bossDefeated: z.boolean().default(false),
    inventory: z.array(itemSchema).max(INVENTORY_SIZE).default([]),
    equipment: z.array(itemSchema).max(SLOTS.length).default([]),
  })
  .superRefine((s, ctx) => {
    const slots = new Set();
    for (const e of s.equipment) {
      if (slots.has(e.slot)) ctx.addIssue({ code: 'custom', path: ['equipment'], message: `two items in slot ${e.slot}` });
      slots.add(e.slot);
    }
    const uids = new Set();
    for (const i of [...s.inventory, ...s.equipment]) {
      if (uids.has(i.uid)) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `duplicate item uid ${i.uid}` });
      uids.add(i.uid);
    }
  });

/** Brand-new level-1 character; empty equipment (the Unity SaveSerializer.Restore refills starter gear, hp/mp 0 => full). */
export function newCharacterState(classId, mapId = 'frostHollow') {
  return stateSchema.parse({
    version: 1, savedAt: 0, classId, mapId, level: 1, exp: 0, gold: 0, hpPotion: 0, mpPotion: 0, upgradeStone: 0,
    hp: 0, mp: 0, x: 0, y: 0, hasPosition: false, bossDefeated: false, inventory: [], equipment: [],
  });
}
