// Playable classes, chosen on New Game. Each class has its own base stats + growth (fed to
// statsForLevel), its own 3 skill-tree branches (skills are matched to a class through their
// branch), its basic-attack style ('melee' sword combo or 'ranged' projectile combo -- magic bolts
// for the wizard, gunshots for the mechanic, see COMBO_CONFIG_BY_CLASS), the class-select style
// line, and its sprite set key (sprite-registry.js `heroes[classId]`).
export const CLASS_DEFINITIONS = Object.freeze({
  warrior: {
    id: 'warrior',
    name: 'Warrior',
    tagline: 'Sword master. Tough, fast combos up close.',
    baseStats: { maxHp: 100, maxMp: 40, atk: 12, def: 4, speed: 80 },
    growth: { maxHp: 12, maxMp: 5, atk: 2.5, def: 1 },
    branches: ['sword', 'body', 'spirit'],
    attackStyle: 'melee',
    styleLabel: 'Close-range sword combos',
  },
  wizard: {
    id: 'wizard',
    name: 'Wizard',
    tagline: 'Spellcaster. Strikes from range with fire, frost and arcane magic.',
    // Squishier, MP-rich and slightly harder-hitting per hit (ranged = safer, so less HP/DEF).
    baseStats: { maxHp: 80, maxMp: 70, atk: 13, def: 3, speed: 80 },
    growth: { maxHp: 9, maxMp: 8, atk: 2.7, def: 0.7 },
    branches: ['fire', 'frost', 'arcane'],
    attackStyle: 'ranged',
    styleLabel: 'Long-range magic bolts',
  },
  mechanic: {
    id: 'mechanic',
    name: 'Mechanic',
    tagline: 'Gunslinging engineer. Rapid shots, rockets and gadgets.',
    // Between the two: sturdier than the wizard, less MP; fast low-damage shots with a cannon finisher.
    baseStats: { maxHp: 90, maxMp: 55, atk: 12, def: 4, speed: 82 },
    growth: { maxHp: 10, maxMp: 6, atk: 2.6, def: 0.9 },
    branches: ['gunnery', 'explosives', 'engineering'],
    attackStyle: 'ranged',
    styleLabel: 'Rapid-fire gunshots',
  },
});

export const CLASS_IDS = Object.freeze(Object.keys(CLASS_DEFINITIONS));
export const DEFAULT_CLASS_ID = 'warrior'; // saves from before classes existed load as warriors

/** Class definition by id; unknown ids fall back to the default class. */
export function getClassDefinition(classId) {
  return CLASS_DEFINITIONS[classId] || CLASS_DEFINITIONS[DEFAULT_CLASS_ID];
}

/** The class that owns a skill-tree branch, or null. */
export function classForBranch(branch) {
  return CLASS_IDS.find((id) => CLASS_DEFINITIONS[id].branches.includes(branch)) || null;
}
