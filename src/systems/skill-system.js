// Skill upgrade rules, passive aggregation and hotbar binding. Pure -> unit-testable.
// `player` shape used here: { level, skillPoints, skills: {skillId: level}, mp, hotbar: [id|null,...] }
import { SKILL_DEFINITIONS, SKILL_ORDER, getSkillValue } from '../data/skill-definitions.js';
import { classForBranch, DEFAULT_CLASS_ID } from '../data/class-definitions.js';

export const HOTBAR_SLOTS = 3;

export function getSkillLevel(player, skillId) {
  return player.skills?.[skillId] || 0;
}

/** Can the player buy the next level of this skill? Returns a reason when refused. */
export function canUpgradeSkill(player, skillId) {
  const def = SKILL_DEFINITIONS[skillId];
  if (!def) return { ok: false, reason: 'Unknown skill' };
  if (classForBranch(def.branch) !== (player.classId || DEFAULT_CLASS_ID)) return { ok: false, reason: 'Other class' };
  const current = getSkillLevel(player, skillId);
  if (current >= def.maxLevel) return { ok: false, reason: 'Max level' };
  if (player.skillPoints < 1) return { ok: false, reason: 'No skill points' };
  for (const req of def.requires || []) {
    if (getSkillLevel(player, req.skill) < req.level) {
      return { ok: false, reason: `Requires ${SKILL_DEFINITIONS[req.skill].name} Lv ${req.level}` };
    }
  }
  const needed = def.requiredLevel[current];
  if (player.level < needed) return { ok: false, reason: `Requires Lv ${needed}` };
  return { ok: true, reason: '' };
}

/** Spends one skill point and raises the skill level. Auto-binds a newly learned active skill
 * to the first empty hotbar slot. Mutates player. */
export function upgradeSkill(player, skillId) {
  const check = canUpgradeSkill(player, skillId);
  if (!check.ok) return check;
  const wasUnlearned = getSkillLevel(player, skillId) === 0;
  player.skills[skillId] = getSkillLevel(player, skillId) + 1;
  player.skillPoints -= 1;
  const def = SKILL_DEFINITIONS[skillId];
  if (wasUnlearned && def.type === 'active' && Array.isArray(player.hotbar)) {
    const emptySlot = player.hotbar.indexOf(null);
    if (emptySlot !== -1) player.hotbar[emptySlot] = skillId;
  }
  return { ok: true, reason: '' };
}

/** Bind (or unbind with skillId=null) an active, learned skill to a hotbar slot. */
export function bindSkillToSlot(player, skillId, slot) {
  if (!Array.isArray(player.hotbar) || slot < 0 || slot >= HOTBAR_SLOTS) return { ok: false, reason: 'Invalid slot' };
  if (skillId === null) {
    player.hotbar[slot] = null;
    return { ok: true, reason: '' };
  }
  const def = SKILL_DEFINITIONS[skillId];
  if (!def || def.type !== 'active') return { ok: false, reason: 'Not an active skill' };
  if (getSkillLevel(player, skillId) < 1) return { ok: false, reason: 'Not learned' };
  player.hotbar[slot] = skillId;
  return { ok: true, reason: '' };
}

/** Can an active skill be cast right now? `cooldowns` maps skillId -> ms remaining. */
export function canCastSkill(player, skillId, cooldowns) {
  const def = SKILL_DEFINITIONS[skillId];
  if (!def || def.type !== 'active') return { ok: false, reason: 'Not castable' };
  const level = getSkillLevel(player, skillId);
  if (level < 1) return { ok: false, reason: 'Not learned' };
  if ((cooldowns[skillId] || 0) > 0) return { ok: false, reason: 'Cooling down' };
  const cost = getSkillValue(skillId, 'mpCost', level);
  if (player.mp < cost) return { ok: false, reason: 'Not enough MP' };
  return { ok: true, reason: '', cost, level };
}

// Defaults for every stat computePassiveModifiers can produce; multipliers start at 1,
// additive/chance stats start at 0. Passives only ever add a bonus on top of these.
const DEFAULT_MODIFIERS = Object.freeze({
  hpMult: 1, mpMult: 1, defAdd: 0, speedMult: 1, comboMult: 0, critChance: 0, regenMult: 1, mpRegenMult: 1,
});

/** Sums every learned passive's contribution into one stat-modifier object. */
export function computePassiveModifiers(player) {
  const mods = { ...DEFAULT_MODIFIERS };
  for (const skillId of SKILL_ORDER) {
    const def = SKILL_DEFINITIONS[skillId];
    if (def.type !== 'passive' || !def.passive) continue;
    const level = getSkillLevel(player, skillId);
    if (level < 1) continue;
    for (const [stat, perLevel] of Object.entries(def.passive)) {
      mods[stat] = (mods[stat] ?? 0) + perLevel[Math.min(level, perLevel.length) - 1];
    }
  }
  return mods;
}

/** Tick all cooldown timers down by dt ms. Mutates `cooldowns`. */
export function tickCooldowns(cooldowns, dt) {
  for (const id of Object.keys(cooldowns)) {
    if (cooldowns[id] > 0) cooldowns[id] = Math.max(0, cooldowns[id] - dt);
  }
}
