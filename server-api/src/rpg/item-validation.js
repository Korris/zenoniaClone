import { z } from 'zod';
import { ALBUM_MILESTONES, APPEARANCE_HAIR_COLORS, APPEARANCE_OUTFIT_COLORS, CARD_COUNT, CARD_DECK_MAX, CARD_IDS, CARD_MAX_STACK, CLASS_IDS, GEM_MAX_ROWS, GEM_SLOTS, HEADGEAR_COUNT, HEADGEAR_TEMPLATE_IDS, INVENTORY_SIZE, MAX_ENHANCE, MAX_INT, MAX_ITEM_SOCKETS, MAX_LEVEL, MAX_STONES, PET_IDS, PET_MAX_DURABILITY, PET_TEAM_SIZE, PET_TEMPLATE_IDS, RARITIES, SKILL_COUNT, SKILL_MAX_RANK, SLOTS } from './rpg-constants.js';

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
  durability: z.number().int().min(0).max(PET_MAX_DURABILITY).optional(), // pets; a pet without it is an old one, at the maximum
  // Socketed Soul Gems "<cardId>:p|g" (Power / Guard), Item.sockets. Absent in old saves = none. The per-slot limit (GEM_SLOTS = GemSockets.SlotCount) is checked right below.
  sockets: z.array(z.string().max(40).regex(/^[a-zA-Z]+:[pg]$/).refine((g) => CARD_IDS.includes(g.split(':')[0]), 'unknown card id')).max(MAX_ITEM_SOCKETS).default([]),
}).refine((i) => i.sockets.length <= (GEM_SLOTS[i.slot] ?? 0), { message: 'more Soul Gem sockets than this slot has' }).transform((i) => ({ ...i, durability: i.durability ?? (i.slot === 'pet' ? PET_MAX_DURABILITY : 0) }));

const int0 = z.number().int().min(0).max(MAX_INT);
const f0 = z.number().finite().min(0).max(1e9);
const coord = z.number().finite().min(-1e6).max(1e6);
const cardId = z.enum(CARD_IDS);
const petIdField = z.union([z.enum(PET_IDS), z.literal('')]).default('');

/** SaveData: the whole persisted character state. Unknown keys are stripped. */
export const stateSchema = z
  .object({
    version: z.number().int().min(0).max(6).default(1), // SaveSerializer.SaveVersion; 6: monster cards (cards / gems / album / cardDeck / cardDaily / cardNpcFirstWins / cardRank) + item sockets; 2: pets carry durability; 3: Frost Hollow 72x48, old section y +384; 4: gacha pets moved to the account, character keeps only petId (which one is worn); 5: petTeam (up to 5 account pet ids), petId still accepted
    savedAt: z.number().int().min(0).default(0),
    classId: z.enum(CLASS_IDS),
    mapId: z.string().min(1).max(32).regex(/^[A-Za-z0-9_-]+$/).default('frostHollow'),
    level: z.number().int().min(1).max(MAX_LEVEL),
    exp: int0, gold: int0, hpPotion: int0, mpPotion: int0,
    // Larger potions and the size on the quick buttons (PotionCatalog). hpPotion / mpPotion stay the Small stacks; absent in old saves = none, Small.
    hpPotionMedium: int0.default(0), hpPotionLarge: int0.default(0), mpPotionMedium: int0.default(0), mpPotionLarge: int0.default(0),
    hpPotionPick: z.number().int().min(0).max(2).default(0), mpPotionPick: z.number().int().min(0).max(2).default(0),
    upgradeStone: z.number().int().min(0).max(MAX_STONES),
    hp: f0, mp: f0, x: coord, y: coord,
    hasPosition: z.boolean().default(false),
    bossDefeated: z.boolean().default(false),
    // SaveData.defeatedBossMaps: map ids whose boss this save killed. Absent in old saves = none. Listed so the strip below keeps it.
    defeatedBossMaps: z.array(z.string().min(1).max(32).regex(/^[A-Za-z0-9_-]+$/)).max(32).default([]),
    // SaveData.quests (QuestLog): quests taken so far; 1 Active, 2 ReadyToTurnIn, 3 Done. Absent in old saves = none. Listed so the strip below keeps it.
    quests: z.array(z.object({
      id: z.string().min(1).max(64).regex(/^[A-Za-z0-9._-]+$/),
      state: z.number().int().min(1).max(3),
      progress: z.number().int().min(0).max(MAX_INT),
    })).max(64).default([]),
    // SaveData.wantedDay (PlayerState.WantedDay): WantedBoss.DayKey of the last daily Wanted Boss claim, 0 = never. Absent in old saves = 0.
    // Listed so the strip above keeps it: without it every relog would allow a new claim. rpg/wanted-day.js stops it going backwards.
    wantedDay: z.number().int().min(0).max(1_000_000).default(0),
    // SaveData.chests / waypoints (Rootburrow V5, PlayerState.Dungeon.cs): dungeon chests still empty for this character (DungeonPoints id + refill time, unix ms)
    // and the attuned warp crystals. Absent in old saves = all chests full, no crystal. Listed so the strip keeps them (a relog must not refill a chest).
    chests: z.array(z.object({
      id: z.string().min(1).max(32).regex(/^[A-Za-z0-9._-]+$/),
      at: z.number().int().min(0),
    })).max(64).default([]),
    waypoints: z.array(z.string().min(1).max(32).regex(/^[A-Za-z0-9._-]+$/)).max(32).default([]),
    // v4: which account gacha pet (AccountState.pets) this character has out, '' = none. Old pet *items* in inventory/equipment above still parse as-is.
    petId: petIdField,
    // v5: PetRules.TeamSize slots (Combat/Support1/Support2/Stat1/Stat2), each an account pet id or '' (empty slot). Absent in old saves = [].
    petTeam: z.array(petIdField).max(PET_TEAM_SIZE).default([]),
    // SaveData.appearance (CharacterAppearanceData): hairstyle, palette indices of hair / outfit colour (-1 = not chosen, the client picks from
    // the name hash), headgear id (0 = none). Absent in older saves = absent here (nothing chosen). Listed so the strip keeps it. No version bump.
    appearance: z.object({
      stored: z.boolean().default(false),
      hairStyle: z.number().int().min(0).max(255).default(0),
      hairColorIndex: z.number().int().min(-1).max(APPEARANCE_HAIR_COLORS - 1).default(-1),
      outfitColorIndex: z.number().int().min(-1).max(APPEARANCE_OUTFIT_COLORS - 1).default(-1),
      equippedHeadgearId: z.number().int().min(0).max(HEADGEAR_COUNT).default(0), // HeadgearCatalog id of the worn hat; the C# side rebuilds it from the headgear slot on load
    }).optional(),
    // Monster cards (SaveData.cards ... cardRank, CardCollection). Absent in old saves = nothing owned. Listed so the strip keeps them: without them every relog loses the collection.
    cards: z.array(z.object({ id: cardId, n: z.number().int().min(1).max(CARD_MAX_STACK), locked: z.number().int().min(0).max(1).default(0) })).max(CARD_COUNT).default([]),
    gems: z.array(z.object({ id: cardId, kind: z.number().int().min(0).max(1), locked: z.boolean().default(false), n: z.number().int().min(1).max(CARD_MAX_STACK) })).max(GEM_MAX_ROWS).default([]),
    album: z.array(z.number().int().min(0).max(CARD_COUNT - 1)).max(CARD_COUNT).default([]), // CardTable indexes ever owned
    albumClaimed: z.number().int().min(0).max((1 << ALBUM_MILESTONES) - 1).default(0), // bit per claimed Album milestone
    cardDeck: z.array(cardId).max(CARD_DECK_MAX).default([]),
    cardDaily: z.object({ day: int0.default(0), npcWins: int0.default(0), pvpRewards: int0.default(0), wagers: int0.default(0) }).default({}),
    cardNpcFirstWins: z.number().int().min(0).max(0xffff).default(0), // bit per duel NPC beaten once
    cardHelp: z.number().int().min(0).max(3).default(0), // bit per first-time card hint shown (bit 0 = guided tour of More > Cards, bit 1 = first PvP risk box confirmed); older saves = 0 = not shown yet
    cardRank: z.object({ season: int0.default(0), mmr: int0.default(0), lp: int0.default(0), wins: int0.default(0), losses: int0.default(0), draws: int0.default(0), best: int0.default(0), prevBest: int0.default(0), rewards: z.number().int().min(0).max(31).default(0) }).default({}), // rewards = bit per tier (Bronze..Diamond) reached at a season end
    inventory: z.array(itemSchema).max(INVENTORY_SIZE).default([]),
    equipment: z.array(itemSchema).max(SLOTS.length).default([]),
    // Skill tree (SaveData.skillRanks / skillSlots): ranks by tree index, magazine slot -> tree index or -1. Absent in old saves = nothing learned.
    skillRanks: z.array(z.number().int().min(0).max(SKILL_MAX_RANK)).max(SKILL_COUNT).default([]),
    skillSlots: z.array(z.number().int().min(-1).max(SKILL_COUNT - 1)).max(SKILL_COUNT).default([]),
  })
  .superRefine((s, ctx) => {
    const slots = new Set();
    for (const e of s.equipment) {
      if (slots.has(e.slot)) ctx.addIssue({ code: 'custom', path: ['equipment'], message: `two items in slot ${e.slot}` });
      slots.add(e.slot);
    }
    const teamIds = s.petTeam.filter((id) => id !== '');
    if (new Set(teamIds).size !== teamIds.length) ctx.addIssue({ code: 'custom', path: ['petTeam'], message: 'duplicate pet in team' });
    const uids = new Set();
    for (const i of [...s.inventory, ...s.equipment]) {
      if (uids.has(i.uid)) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `duplicate item uid ${i.uid}` });
      uids.add(i.uid);
      // Pets: a known kind, any class, never enhanced (ItemFactory.Sanitize drops the rest on the C# side).
      const isPetTemplate = PET_TEMPLATE_IDS.includes(i.templateId);
      if ((i.slot === 'pet') !== isPetTemplate) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `item ${i.uid}: template ${i.templateId} does not fit slot ${i.slot}` });
      if (i.slot === 'pet' && (i.enhance !== 0 || i.classId !== '')) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `pet ${i.uid} cannot be enhanced or tied to a class` });
      // Hats (HeadgearCatalog): a known hat template in the headgear slot only, cosmetic (no stats), any class, never enhanced.
      const isHatTemplate = HEADGEAR_TEMPLATE_IDS.includes(i.templateId);
      if ((i.slot === 'headgear') !== isHatTemplate) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `item ${i.uid}: template ${i.templateId} does not fit slot ${i.slot}` });
      if (i.slot === 'headgear' && (i.enhance !== 0 || i.classId !== '' || Object.values(i.stats).some((v) => v !== 0))) ctx.addIssue({ code: 'custom', path: ['inventory'], message: `hat ${i.uid} carries no stats, class or enhance` });
    }
    // 1 SP per level: spent points can never exceed the level (the zone server re-sanitizes prerequisites via SkillRanks.Sanitize).
    const spent = s.skillRanks.reduce((a, b) => a + b, 0);
    if (spent > s.level) ctx.addIssue({ code: 'custom', path: ['skillRanks'], message: `${spent} skill ranks exceed level ${s.level}` });
    const placed = s.skillSlots.filter((v) => v >= 0);
    if (new Set(placed).size !== placed.length) ctx.addIssue({ code: 'custom', path: ['skillSlots'], message: 'duplicate skill in magazine' });
    for (const v of placed) {
      if (!(s.skillRanks[v] > 0)) ctx.addIssue({ code: 'custom', path: ['skillSlots'], message: `skill ${v} is not learned` });
    }
  });

/**
 * Brand-new level-1 character; empty equipment (the Unity SaveSerializer.Restore refills starter gear, hp/mp 0 => full). Begins in Oakhaven, like an offline hero.
 * Only a new character gets this: stored characters keep their mapId, and the schema's 'frostHollow' default above stays for old saves that have none.
 */
export function newCharacterState(classId, mapId = 'oakhavenVillage') {
  return stateSchema.parse({
    version: 1, savedAt: 0, classId, mapId, level: 1, exp: 0, gold: 0, hpPotion: 0, mpPotion: 0, upgradeStone: 0,
    hp: 0, mp: 0, x: 0, y: 0, hasPosition: false, bossDefeated: false, inventory: [], equipment: [],
    skillRanks: [1, 0, 0, 0, 0, 0, 0, 0], skillSlots: [0, -1, -1, -1, -1, -1, -1, -1], // new characters start with skill 1 learned (Lv 1 point)
  });
}
