// Equipment data: slots, rarities, and the weapon/armor templates drops are rolled from.
// Pure config, no DOM. Item instance shape produced by equipment-system.js:
// { uid, templateId, slot, name, rarity, itemLevel, stats }
export const EQUIPMENT_SLOTS = Object.freeze(['weapon', 'helmet', 'armor', 'gloves', 'boots']);

// mult scales stat rolls; weight is the drop-chance share (sums to 1); prefix decorates the name.
export const RARITIES = Object.freeze({
  common: { id: 'common', color: '#ffffff', mult: 1.0, weight: 0.70, prefix: null },
  uncommon: { id: 'uncommon', color: '#8fff8f', mult: 1.25, weight: 0.20, prefix: 'Fine' },
  rare: { id: 'rare', color: '#6ba8ff', mult: 1.6, weight: 0.08, prefix: 'Superior' },
  epic: { id: 'epic', color: '#e0a0ff', mult: 2.1, weight: 0.02, prefix: 'Legendary' },
});
export const RARITY_ORDER = Object.freeze(['common', 'uncommon', 'rare', 'epic']);

/** Shared art tier: 1/5, 10/15 ... 90/95. Values between each pair stay in that band. */
export function equipmentTier(itemLevel) {
  return Math.max(0, Math.min(9, Math.floor((Number.isFinite(itemLevel) ? itemLevel : 1) / 10)));
}

export const ARMOR_TIER_NAMES = Object.freeze({
  helmet: ['Leather Cap', 'Steel Helm', 'Bronze Helm', 'Frost Helm', 'Flame Helm', 'Royal Helm', 'Verdant Helm', 'Arcane Helm', 'Celestial Helm', 'Dragon Helm'],
  armor: ['Leather Vest', 'Steel Plate', 'Bronze Plate', 'Frost Plate', 'Flame Plate', 'Royal Plate', 'Verdant Armor', 'Arcane Armor', 'Celestial Armor', 'Dragon Armor'],
  gloves: ['Cloth Gloves', 'Steel Gauntlets', 'Bronze Gauntlets', 'Frost Gauntlets', 'Flame Gauntlets', 'Royal Gauntlets', 'Verdant Gloves', 'Arcane Gauntlets', 'Celestial Gauntlets', 'Dragon Gauntlets'],
  boots: ['Leather Boots', 'Steel Boots', 'Bronze Boots', 'Frost Boots', 'Flame Boots', 'Royal Boots', 'Verdant Boots', 'Arcane Boots', 'Celestial Boots', 'Dragon Boots'],
});

const weapon = (id, name, sprite, atk, minItemLevel) => ({
  id, slot: 'weapon', name, sprite, minItemLevel, baseStats: { atk },
});
const gear = (slot, id, name, sprite, minItemLevel, baseStats) => ({
  id, slot, name, sprite, minItemLevel, baseStats,
});

// 9 weapon tiers; minItemLevel gates which monsters can drop them (see monster-definitions.js
// `itemLevel`), roughly ordered by baseAtk so stronger swords show up later.
export const WEAPON_TEMPLATES = Object.freeze({
  sword: weapon('sword', 'Sword', 'itemSword', 4, 1),
  sword2: weapon('sword2', 'Short Sword', 'itemSword2', 6, 1),
  rapier: weapon('rapier', 'Rapier', 'itemRapier', 7, 3),
  club: weapon('club', 'Club', 'itemClub', 8, 4),
  katana: weapon('katana', 'Katana', 'itemKatana', 9, 6),
  axe: weapon('axe', 'Axe', 'itemAxe', 10, 6),
  lance: weapon('lance', 'Lance', 'itemLance', 11, 8),
  hammer: weapon('hammer', 'Hammer', 'itemHammer', 13, 11),
  bigSword: weapon('bigSword', 'Big Sword', 'itemBigSword', 15, 14),
});

// Legacy stat templates remain save-compatible. helmet/armor give def+hp; gloves atk+critChance;
// boots def+speedMult. Painted icons and new item names use the ten shared itemLevel bands above.
export const ARMOR_TEMPLATES = Object.freeze({
  leatherCap: gear('helmet', 'leatherCap', 'Leather Cap', 'iconHelmet', 1, { def: 1, maxHp: 5 }),
  ironHelm: gear('helmet', 'ironHelm', 'Iron Helm', 'iconHelmet', 5, { def: 3, maxHp: 12 }),
  knightHelm: gear('helmet', 'knightHelm', 'Knight Helm', 'iconHelmet', 10, { def: 6, maxHp: 22 }),

  leatherVest: gear('armor', 'leatherVest', 'Leather Vest', 'iconArmor', 1, { def: 2, maxHp: 10 }),
  ironMail: gear('armor', 'ironMail', 'Iron Mail', 'iconArmor', 5, { def: 5, maxHp: 25 }),
  knightPlate: gear('armor', 'knightPlate', 'Knight Plate', 'iconArmor', 10, { def: 9, maxHp: 45 }),

  clothGloves: gear('gloves', 'clothGloves', 'Cloth Gloves', 'iconGuard', 1, { atk: 1, critChance: 0.02 }),
  leatherGloves: gear('gloves', 'leatherGloves', 'Leather Gloves', 'iconGuard', 5, { atk: 2, critChance: 0.04 }),
  ironGauntlets: gear('gloves', 'ironGauntlets', 'Iron Gauntlets', 'iconGuard', 10, { atk: 4, critChance: 0.07 }),

  leatherBoots: gear('boots', 'leatherBoots', 'Leather Boots', 'iconBoot', 1, { def: 1, speedMult: 0.03 }),
  swiftBoots: gear('boots', 'swiftBoots', 'Swift Boots', 'iconBoot', 5, { def: 2, speedMult: 0.06 }),
  knightGreaves: gear('boots', 'knightGreaves', 'Knight Greaves', 'iconBoot', 10, { def: 4, speedMult: 0.09 }),
});

/** All templates keyed by id (flat lookup for sanitize/sprite registry). */
export const ITEM_TEMPLATES = Object.freeze({ ...WEAPON_TEMPLATES, ...ARMOR_TEMPLATES });

/** Templates grouped by slot, used by generateItem() to pick a candidate pool. */
export const TEMPLATES_BY_SLOT = Object.freeze(
  EQUIPMENT_SLOTS.reduce((acc, slot) => {
    acc[slot] = Object.values(ITEM_TEMPLATES).filter((t) => t.slot === slot);
    return acc;
  }, {}),
);
