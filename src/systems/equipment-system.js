// Equipment generation, inventory management and stat aggregation. Pure -> unit-testable
// with an injected RNG (same `rand` convention as loot-system.js / damage-formula.js).
import { MAX_ENHANCE } from '../data/weapon-enhance-config.js';
import { effectiveItemStats } from './weapon-enhance-stats.js';
import { EQUIPMENT_SLOTS, RARITIES, RARITY_ORDER, ITEM_TEMPLATES, TEMPLATES_BY_SLOT, ARMOR_TIER_NAMES, OFFHAND_BY_CLASS, equipmentTier } from '../data/item-definitions.js';

export const INVENTORY_SIZE = 20;
const NORMAL_DROP_CHANCE = 0.12;
// Temporary playtest switch: every kill drops a class weapon. Disable to restore normal loot.
export const GUARANTEED_WEAPON_DROPS = false;

/** Rolls a rarity id using the weighted table in RARITIES (weights sum to 1). */
export function rollRarity(rand = Math.random) {
  const roll = rand();
  let cumulative = 0;
  for (const id of RARITY_ORDER) {
    cumulative += RARITIES[id].weight;
    if (roll < cumulative) return id;
  }
  return RARITY_ORDER[RARITY_ORDER.length - 1];
}

const WEAPON_CLASS_IDS = ['warrior', 'wizard', 'mechanic', 'cleric', 'assassin'];
const offhandClass = (classId) => (OFFHAND_BY_CLASS[classId] ? classId : 'warrior');
// Offhand template type -> owning class; the template (validated) decides, never the raw save tag.
const OFFHAND_CLASS_BY_TYPE = Object.freeze(Object.fromEntries(Object.entries(OFFHAND_BY_CLASS).map(([cls, o]) => [o.type, cls])));

/** Picks a template for `slot` among those unlocked at `itemLevel` (falls back to the weakest tier). */
function pickTemplate(slot, itemLevel, rand, classId = 'warrior') {
  const candidates = TEMPLATES_BY_SLOT[slot].filter((t) => t.minItemLevel <= itemLevel
    && (slot !== 'offhand' || t.id === OFFHAND_BY_CLASS[offhandClass(classId)].type));
  const pool = candidates.length ? candidates : [TEMPLATES_BY_SLOT[slot][0]];
  return pool[Math.floor(rand() * pool.length)];
}

/**
 * Generates one equipment item instance. Stats = round(base * rarityMult * (1 + 0.08*(itemLevel-1))).
 * @param {() => number} rand injectable RNG for deterministic tests
 * @param {number} itemLevel gates which templates can be picked and scales stat rolls
 * @param {string|null} slot fixed slot, or null to pick one at random
 * @param {string|null} forceRarity skip the rarity roll (used for guaranteed boss rare/epic drops)
 */
export function generateItem(rand, itemLevel, slot = null, forceRarity = null, classId = 'warrior') {
  const chosenSlot = slot || EQUIPMENT_SLOTS[Math.floor(rand() * EQUIPMENT_SLOTS.length)];
  const template = pickTemplate(chosenSlot, itemLevel, rand, classId);
  const rarity = forceRarity || rollRarity(rand);
  const rarityMult = RARITIES[rarity].mult;
  const levelMult = 1 + 0.08 * (itemLevel - 1);
  const stats = {};
  for (const [stat, base] of Object.entries(template.baseStats)) {
    const value = base * rarityMult * levelMult;
    // Fractional stats (critChance, speedMult) keep 3 decimals; whole-number stats round as before.
    stats[stat] = Number.isInteger(base) ? Math.round(value) : Math.round(value * 1000) / 1000;
  }
  const prefix = RARITIES[rarity].prefix;
  const weaponType = { warrior: 'Sword', wizard: 'Staff', mechanic: 'Gun', cleric: 'Holy Staff', assassin: 'Dagger' }[classId] || 'Sword';
  const baseName = chosenSlot === 'weapon' ? weaponType
    : chosenSlot === 'offhand' ? OFFHAND_BY_CLASS[offhandClass(classId)].name
      : ARMOR_TIER_NAMES[chosenSlot]?.[equipmentTier(itemLevel)] || template.name;
  const name = prefix ? `${prefix} ${baseName}` : baseName;
  const uid = `${template.id}-${Math.floor(rand() * 1e9).toString(36)}`;
  const item = { uid, templateId: template.id, slot: chosenSlot, name, rarity, itemLevel, stats };
  if (chosenSlot === 'weapon') item.classId = WEAPON_CLASS_IDS.includes(classId) ? classId : 'warrior';
  if (chosenSlot === 'offhand') item.classId = offhandClass(classId);
  if (chosenSlot === 'weapon' || chosenSlot === 'offhand') item.enhance = 0; // constant, no rand() so seeded generation stays unchanged
  return item;
}

/**
 * Rolls equipment on death; the temporary playtest switch guarantees a weapon on every kill.
 * Bosses keep their guaranteed rare-or-epic quality.
 * @param {object} monsterDef from data/monster-definitions.js (`itemLevel`, `behavior`)
 * @returns {object|null} an item instance, or null when nothing dropped
 */
export function rollEquipmentDrop(monsterDef, rand = Math.random) {
  const isBoss = monsterDef.behavior === 'boss' || Boolean(monsterDef.isBoss);
  if (!GUARANTEED_WEAPON_DROPS && !isBoss && rand() >= NORMAL_DROP_CHANCE) return null;
  const itemLevel = monsterDef.itemLevel || 1;
  const forceRarity = isBoss ? (rand() < 0.5 ? 'rare' : 'epic') : null;
  // Class gear drops for any class (not just the killer's) so players can trade it later.
  const dropClass = WEAPON_CLASS_IDS[Math.floor(rand() * WEAPON_CLASS_IDS.length)];
  return generateItem(rand, itemLevel, GUARANTEED_WEAPON_DROPS ? 'weapon' : null, forceRarity, dropClass);
}

/** Sums every equipped item's stats into the additive/multiplicative bundle player.recomputeStats() applies. */
export function computeEquipmentModifiers(player) {
  const mods = { atkAdd: 0, defAdd: 0, hpAdd: 0, mpAdd: 0, speedMult: 1, critChance: 0 };
  for (const slot of EQUIPMENT_SLOTS) {
    const item = player.equipment?.[slot];
    if (!item) continue;
    const stats = effectiveItemStats(item);
    if (stats.atk) mods.atkAdd += stats.atk;
    if (stats.def) mods.defAdd += stats.def;
    if (stats.maxHp) mods.hpAdd += stats.maxHp;
    if (stats.maxMp) mods.mpAdd += stats.maxMp;
    if (stats.speedMult) mods.speedMult += stats.speedMult;
    if (stats.critChance) mods.critChance += stats.critChance;
  }
  return mods;
}

/** Adds an item to the bag. Returns false (no mutation) when the bag is full. */
export function addToInventory(player, item) {
  if (player.inventory.length >= INVENTORY_SIZE) return false;
  player.inventory.push(item);
  return true;
}

/** Removes an item by uid. Returns false when it wasn't found. */
export function removeFromInventory(player, uid) {
  const index = player.inventory.findIndex((i) => i.uid === uid);
  if (index === -1) return false;
  player.inventory.splice(index, 1);
  return true;
}

/**
 * Equips `item` (must already be in the bag, or freshly generated), swapping whatever was in
 * that slot back into the bag. Caller is responsible for calling player.recomputeStats() after
 * (mirrors the skill-system.js convention: systems mutate data, UI recomputes derived stats).
 */
/** Class gear (weapon/offhand carry classId) only fits its own class; armor fits everyone. */
export function canEquipForClass(player, item) {
  return !item.classId || !player.classId || item.classId === player.classId;
}

export function equipItem(player, item) {
  if (!EQUIPMENT_SLOTS.includes(item.slot)) return { ok: false, reason: 'Invalid slot' };
  if (!canEquipForClass(player, item)) return { ok: false, reason: 'Wrong class' };
  const wasInInventory = player.inventory.some((i) => i.uid === item.uid);
  const previous = player.equipment[item.slot];
  const sizeAfter = player.inventory.length - (wasInInventory ? 1 : 0) + (previous ? 1 : 0);
  if (sizeAfter > INVENTORY_SIZE) return { ok: false, reason: 'Inventory full' };
  if (wasInInventory) removeFromInventory(player, item.uid);
  player.equipment[item.slot] = item;
  if (previous) addToInventory(player, previous);
  return { ok: true, reason: '' };
}

/** Unequips a slot back into the bag (fails if the bag has no room). */
export function unequipItem(player, slot) {
  const item = player.equipment[slot];
  if (!item) return { ok: false, reason: 'Empty slot' };
  if (player.inventory.length >= INVENTORY_SIZE) return { ok: false, reason: 'Inventory full' };
  player.equipment[slot] = null;
  addToInventory(player, item);
  return { ok: true, reason: '' };
}

/** True when `item` is strictly better than `equipped` by total stat value (simple power heuristic). */
export function isUpgrade(item, equipped) {
  if (!equipped) return true;
  const score = (it) => Object.values(effectiveItemStats(it)).reduce((sum, v) => sum + v, 0);
  return score(item) > score(equipped);
}

/** Validates an item coming from untrusted save JSON. Returns null when it can't be trusted. */
export function sanitizeItem(raw) {
  if (!raw || typeof raw !== 'object') return null;
  const template = ITEM_TEMPLATES[raw.templateId];
  if (!template || raw.slot !== template.slot || !RARITIES[raw.rarity]) return null;
  const itemLevel = Number.isFinite(raw.itemLevel) ? Math.max(1, Math.min(95, Math.floor(raw.itemLevel))) : 1;
  const stats = {};
  for (const stat of Object.keys(template.baseStats)) {
    const value = raw.stats?.[stat];
    if (Number.isFinite(value)) stats[stat] = Math.max(0, Math.min(999, Math.round(value * 1000) / 1000));
  }
  const uid = typeof raw.uid === 'string' && raw.uid ? raw.uid : `${template.id}-${Math.random().toString(36).slice(2, 10)}`;
  const name = typeof raw.name === 'string' && raw.name ? raw.name.slice(0, 40) : template.name;
  const item = { uid, templateId: template.id, slot: template.slot, name, rarity: raw.rarity, itemLevel, stats };
  if (template.slot === 'weapon' && WEAPON_CLASS_IDS.includes(raw.classId)) item.classId = raw.classId;
  if (template.slot === 'offhand') item.classId = OFFHAND_CLASS_BY_TYPE[template.id] || 'warrior';
  if (template.slot === 'weapon' || template.slot === 'offhand') {
    item.enhance = Number.isInteger(raw.enhance) ? Math.max(0, Math.min(MAX_ENHANCE, raw.enhance)) : 0;
  }
  return item;
}
