// Converts game state to/from a plain JSON object and validates loaded data.
// Pure (no localStorage) so it is unit-testable; storage lives in local-storage-save.js.
import { MAX_LEVEL } from '../data/hero-stats-config.js';
import { SKILL_DEFINITIONS } from '../data/skill-definitions.js';
import { HOTBAR_SLOTS } from '../systems/skill-system.js';
import { sanitizeQuestState } from '../systems/quest-system.js';
import { EQUIPMENT_SLOTS } from '../data/item-definitions.js';
import { sanitizeItem, INVENTORY_SIZE } from '../systems/equipment-system.js';
import { CLASS_DEFINITIONS, DEFAULT_CLASS_ID, classForBranch } from '../data/class-definitions.js';

export const SAVE_VERSION = 1;

/**
 * @param {{player:object, mapId:string, x:number, y:number, bossDefeated:boolean, quests?:object}} state
 */
export function serializeSave(state) {
  return {
    version: SAVE_VERSION,
    savedAt: Date.now(),
    player: state.player.toSaveData(),
    mapId: state.mapId,
    x: Math.round(state.x),
    y: Math.round(state.y),
    bossDefeated: Boolean(state.bossDefeated),
    quests: state.quests ? { active: { ...state.quests.active }, completed: [...state.quests.completed] } : undefined,
  };
}

const isNum = (v) => typeof v === 'number' && Number.isFinite(v);

/** Returns a sanitized save object, or null if the data is unusable. */
export function validateSave(data) {
  if (!data || typeof data !== 'object' || data.version !== SAVE_VERSION) return null;
  const p = data.player;
  if (!p || !isNum(p.level) || !isNum(p.exp) || !isNum(p.skillPoints) || typeof data.mapId !== 'string') return null;

  // Saves from before classes existed have no classId -> warrior (the only class back then).
  const classId = CLASS_DEFINITIONS[p.classId] ? p.classId : DEFAULT_CLASS_ID;
  const skills = {};
  for (const [id, lvl] of Object.entries(p.skills || {})) {
    const def = SKILL_DEFINITIONS[id];
    // Skills of another class are dropped: a tampered save can't mix class trees.
    if (def && isNum(lvl) && classForBranch(def.branch) === classId) skills[id] = Math.max(0, Math.min(def.maxLevel, Math.floor(lvl)));
  }

  // Only keep hotbar slots pointing at a known, learned active skill; never trust raw client JSON.
  const rawHotbar = Array.isArray(p.hotbar) ? p.hotbar : [];
  const hotbar = new Array(HOTBAR_SLOTS).fill(null).map((_, i) => {
    const id = rawHotbar[i];
    const def = typeof id === 'string' ? SKILL_DEFINITIONS[id] : null;
    return def && def.type === 'active' && skills[id] > 0 ? id : null;
  });

  // Items are regenerated through sanitizeItem() -- never trust templateId/slot/rarity/stats from raw JSON.
  const inventory = Array.isArray(p.inventory)
    ? p.inventory.map(sanitizeItem).filter(Boolean).slice(0, INVENTORY_SIZE)
    : [];
  const equipment = {};
  for (const slot of EQUIPMENT_SLOTS) {
    const item = p.equipment?.[slot] ? sanitizeItem(p.equipment[slot]) : null;
    equipment[slot] = item && item.slot === slot ? item : null;
  }

  return {
    version: SAVE_VERSION,
    savedAt: isNum(data.savedAt) ? data.savedAt : 0,
    player: {
      classId,
      level: Math.max(1, Math.min(MAX_LEVEL, Math.floor(p.level))),
      exp: Math.max(0, Math.floor(p.exp)),
      skillPoints: Math.max(0, Math.floor(p.skillPoints)),
      skills,
      hotbar,
      gold: isNum(p.gold) ? Math.max(0, Math.floor(p.gold)) : 0,
      hp: isNum(p.hp) && p.hp > 0 ? p.hp : undefined, // never load a dead hero
      mp: isNum(p.mp) ? p.mp : undefined,
      inventory,
      equipment,
    },
    mapId: data.mapId,
    x: isNum(data.x) ? data.x : null,
    y: isNum(data.y) ? data.y : null,
    bossDefeated: Boolean(data.bossDefeated),
    quests: sanitizeQuestState(data.quests),
  };
}
