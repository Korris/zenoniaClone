// Mirrors Assets/Scripts/Rpg (RpgClasses.Ids, Slots, ItemDefs.RarityOrder, PlayerState, Leveling, EnhanceConfig). Keep in sync by hand.

export const CLASS_IDS = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin', 'fighter', 'archer']; // fighter added 2026-11-04; several heroes of one class are allowed (2026-11-12); archer 2026-11-26
export const DEFAULT_MAX_CHARACTERS = 6; // heroes one account may hold, any classes (AccountFlow.MaxCharacters on the client); MAX_CHARACTERS overrides it
export const SLOTS = ['weapon', 'helmet', 'armor', 'gloves', 'boots', 'offhand', 'pet', 'headgear']; // headgear (19/11/2026): cosmetic hat, HeadgearCatalog
export const PET_MAX_DURABILITY = 1000; // PetConfig.MaxDurability
/** HeadgearCatalog.All keys in id order (id = index + 1; 0 = bare head). Only append. Hat items: templateId 'hat-<key>', slot 'headgear', no stats, no class, never enhanced. */
export const HEADGEAR_KEYS = [
  'iron-helmet', 'wizard-hat', 'copper-goggles', 'ninja-headband', 'nun-veil', 'straw-hat', 'cloth-headband', 'pompom-beanie', 'aviator-cap', 'bunny-ears',
  'slime-cap', 'fox-ears', 'devil-horns', 'frog-hat', 'wildflower-wreath', 'silver-wolf-crown', 'ice-crystal-tiara', 'serpent-crown', 'ultimate-wizard-hat',
];
export const HEADGEAR_COUNT = HEADGEAR_KEYS.length;
export const HEADGEAR_TEMPLATE_IDS = HEADGEAR_KEYS.map((k) => `hat-${k}`);
export const PET_TEMPLATE_IDS = ['pet-spirit', 'pet-imp']; // PetConfig.All: the only things the pet slot holds. Their bonuses live in C#, these items carry no stats.
export const RARITIES = ['common', 'uncommon', 'rare', 'epic'];
export const ENHANCEABLE_SLOTS = ['weapon', 'offhand'];
/** EquipLevel.ByTier: hero level needed to equip gear of art tier itemLevel / 10 (clamped 0..9): T0 1, T(n) = n * 10 (16/11/2026). Every slot but pet; enhancement never raises it. */
export const EQUIP_LEVEL_BY_TIER = [1, 10, 20, 30, 40, 50, 60, 70, 80, 90];
export const requiredEquipLevel = (item) =>
  SLOTS.includes(item.slot) && item.slot !== 'pet' && item.slot !== 'headgear' ? EQUIP_LEVEL_BY_TIER[Math.min(9, Math.max(0, Math.floor(item.itemLevel / 10)))] : 1;

export const MAX_LEVEL = 90; // Leveling.MaxLevel (60 until 16/11/2026)
export const SKILL_COUNT = 8; // SkillDef.PerClass: skills per class tree
export const SKILL_MAX_RANK = 10; // SkillDef.MaxRank
export const INVENTORY_SIZE = 50; // PlayerState.InventorySize (was 20 until 2026-11-08)
export const APPEARANCE_HAIR_COLORS = 9, APPEARANCE_OUTFIT_COLORS = 6; // CharacterAppearanceData.HairColorCount / OutfitColorCount (Data/hero-layered/palettes.json)
export const MAX_STONES = 9999; // PlayerState.MaxStones
export const MAX_INT = 2_000_000_000; // C# int fields (gold, exp, potions) must stay inside Int32

export const MAX_ENHANCE = 15; // EnhanceConfig.MaxEnhance
export const OFFHAND_SCALE = 0.5; // EnhanceStats.OffhandScale
/** StoneUpgradeMethod.SuccessByTarget: chance to reach the target level (index = target level; +1..+3 guaranteed). */
export const SUCCESS_BY_TARGET = [0, 1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2];
export const STONE_COST = 1; // spent on failure too

// PetConfig.All ids (account-level gacha companions; distinct from PET_TEMPLATE_IDS, the item slot). Fixed order = gacha pool order, keep in sync.
export const PET_IDS = ['spirit', 'imp', 'frostSlime', 'hornRabbit', 'iceGoblin', 'thornBoar', 'spectralWolf', 'marshStalker', 'crystalGolem', 'frostfangKing'];
export const ACCOUNT_HISTORY_MAX = 200; // AccountState.history cap
export const PET_MAX_LEVEL = 60; // PetRules.MaxLevel (OwnedPet.level, Pet Food)
export const PET_MAX_STARS = 5; // PetRules.MaxStars (OwnedPet.stars, shards + gold)
export const PET_TEAM_SIZE = 5; // PetRules.TeamSize (SaveData.petTeam slots)

// Monster cards (Assets/Scripts/Rpg/Cards/CardTable.cs). card-ids.json is generated from the C# table; ZenoniaSkills.Tests CardIdsExportTests fails when they differ.
import cardTable from './card-ids.json' with { type: 'json' };
export const CARD_IDS = cardTable.map((c) => c.id); // album index = position, append only
export const CARD_COUNT = CARD_IDS.length; // CardTable.Count (53: 40 pre-Taurus + 12 Taurus V4 + the Matriarch field boss card, appended)
export const CARD_MAX_STACK = 999; // CardTable.MaxStack: copies of one card / of one gem stack
export const GEM_MAX_ROWS = 160; // CardCollection.MaxGemRows
export const CARD_DECK_MAX = 8; // CardCollection.MaxDeck
export const ALBUM_MILESTONES = 4; // AlbumRules.Milestones: albumClaimed is a 4-bit mask
export const MAX_ITEM_SOCKETS = 2; // ItemFactory.MaxSockets
export const GEM_SLOTS = { weapon: 2, armor: 2, helmet: 1 }; // GemSockets.SlotCount: sockets per item slot, every other slot has none
