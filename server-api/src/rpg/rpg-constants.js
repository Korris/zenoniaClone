// Mirrors Assets/Scripts/Rpg (RpgClasses.Ids, Slots, ItemDefs.RarityOrder, PlayerState, Leveling, EnhanceConfig). Keep in sync by hand.

export const CLASS_IDS = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin']; // one character per class => max 5 per account
export const SLOTS = ['weapon', 'helmet', 'armor', 'gloves', 'boots', 'offhand'];
export const RARITIES = ['common', 'uncommon', 'rare', 'epic'];
export const ENHANCEABLE_SLOTS = ['weapon', 'offhand'];

export const MAX_LEVEL = 60; // Leveling.MaxLevel
export const SKILL_COUNT = 8; // SkillDef.PerClass: skills per class tree
export const SKILL_MAX_RANK = 10; // SkillDef.MaxRank
export const INVENTORY_SIZE = 20; // PlayerState.InventorySize
export const MAX_STONES = 9999; // PlayerState.MaxStones
export const MAX_INT = 2_000_000_000; // C# int fields (gold, exp, potions) must stay inside Int32

export const MAX_ENHANCE = 15; // EnhanceConfig.MaxEnhance
export const OFFHAND_SCALE = 0.5; // EnhanceStats.OffhandScale
/** StoneUpgradeMethod.SuccessByTarget: chance to reach the target level (index = target level; +1..+3 guaranteed). */
export const SUCCESS_BY_TARGET = [0, 1, 1, 1, 0.9, 0.85, 0.8, 0.7, 0.65, 0.6, 0.5, 0.45, 0.4, 0.3, 0.25, 0.2];
export const STONE_COST = 1; // spent on failure too
