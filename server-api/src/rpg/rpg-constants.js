// Mirrors Assets/Scripts/Rpg (RpgClasses.Ids, Slots, ItemDefs.RarityOrder, PlayerState, Leveling, EnhanceConfig). Keep in sync by hand.

export const CLASS_IDS = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin', 'fighter']; // one character per class => max 6 per account (fighter added 2026-11-04)
export const SLOTS = ['weapon', 'helmet', 'armor', 'gloves', 'boots', 'offhand', 'pet'];
export const PET_MAX_DURABILITY = 1000; // PetConfig.MaxDurability
export const PET_TEMPLATE_IDS = ['pet-spirit', 'pet-imp']; // PetConfig.All: the only things the pet slot holds. Their bonuses live in C#, these items carry no stats.
export const RARITIES = ['common', 'uncommon', 'rare', 'epic'];
export const ENHANCEABLE_SLOTS = ['weapon', 'offhand'];
/** EquipLevel.ByTier: hero level needed to equip a weapon / offhand of art tier itemLevel / 10 (clamped 0..9). Enhancement never raises it; armor and pets have none. */
export const EQUIP_LEVEL_BY_TIER = [1, 5, 10, 15, 20, 30, 35, 40, 50, 60];
export const requiredEquipLevel = (item) =>
  ENHANCEABLE_SLOTS.includes(item.slot) ? EQUIP_LEVEL_BY_TIER[Math.min(9, Math.max(0, Math.floor(item.itemLevel / 10)))] : 1;

export const MAX_LEVEL = 60; // Leveling.MaxLevel
export const SKILL_COUNT = 8; // SkillDef.PerClass: skills per class tree
export const SKILL_MAX_RANK = 10; // SkillDef.MaxRank
export const INVENTORY_SIZE = 50; // PlayerState.InventorySize (was 20 until 2026-11-08)
export const MAX_STONES = 9999; // PlayerState.MaxStones
export const MAX_INT = 2_000_000_000; // C# int fields (gold, exp, potions) must stay inside Int32

export const MAX_ENHANCE = 15; // EnhanceConfig.MaxEnhance
export const OFFHAND_SCALE = 0.5; // EnhanceStats.OffhandScale
/** StoneUpgradeMethod.SuccessByTarget: chance to reach the target level (index = target level; +1..+3 guaranteed). */
export const SUCCESS_BY_TARGET = [0, 1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2];
export const STONE_COST = 1; // spent on failure too
