// Pure layout + formatting helpers for the skill tree panel. No DOM/canvas calls here so the
// positions are easy to reason about; skill-tree-panel.js does the actual drawing.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { SKILL_DEFINITIONS, SKILL_ORDER, getSkillValue } from '../data/skill-definitions.js';
import { getSkillLevel } from '../systems/skill-system.js';

export const PANEL_W = 860;
export const PANEL_H = 540;
export const PANEL_X = (CANVAS_WIDTH - PANEL_W) / 2;
export const PANEL_Y = (CANVAS_HEIGHT - PANEL_H) / 2;
export const NODE_SIZE = 64;
export const TIER_ROW_H = 130;
export const TREE_TOP = PANEL_Y + 46;
export const INFO_BOX_Y = PANEL_Y + 436;
export const INFO_BOX_H = 76;
export const BRANCHES_PER_CLASS = 3;
export const COLUMN_W = PANEL_W / BRANCHES_PER_CLASS;

const SUBCOL_FRACTION = { 0: 0.30, 1: 0.70, single: 0.5 };

/** branch -> ordered list of its skill ids (tier1 x2, tier2 x2, tier3 x1), per SKILL_ORDER. */
export function skillsForBranch(branch) {
  return SKILL_ORDER.filter((id) => SKILL_DEFINITIONS[id].branch === branch);
}

/** skillId -> {x, y} center of its node box, skillId -> {branchIndex, nodeIndex} for navigation,
 * for one class's `branches` (CLASS_DEFINITIONS[classId].branches). */
export function computeSkillTreeLayout(branches) {
  const positions = {};
  const navIndex = {}; // skillId -> {branchIndex, nodeIndex} (nodeIndex = order within branch, 0-4)
  branches.forEach((branch, branchIndex) => {
    const ids = skillsForBranch(branch);
    const colX = PANEL_X + branchIndex * COLUMN_W;
    const seenInTier = {}; // tier -> how many siblings already placed (assigns subcol 0, then 1)
    ids.forEach((id, nodeIndex) => {
      const def = SKILL_DEFINITIONS[id];
      const tierSiblingCount = ids.filter((other) => SKILL_DEFINITIONS[other].tier === def.tier).length;
      const subcol = tierSiblingCount > 1 ? (seenInTier[def.tier] = (seenInTier[def.tier] || 0) + 1) - 1 : 'single';
      const x = colX + COLUMN_W * SUBCOL_FRACTION[subcol];
      const y = TREE_TOP + (def.tier - 1) * TIER_ROW_H + NODE_SIZE / 2;
      positions[id] = { x, y };
      navIndex[id] = { branchIndex, nodeIndex };
    });
  });
  return { positions, navIndex };
}

/** Node visual state, in priority order: maxed > available > learned > locked. */
export function describeNodeState(player, skillId) {
  const def = SKILL_DEFINITIONS[skillId];
  const level = getSkillLevel(player, skillId);
  const maxed = level >= def.maxLevel;
  const prereqsMet = (def.requires || []).every((r) => getSkillLevel(player, r.skill) >= r.level);
  const heroLevelMet = maxed || player.level >= def.requiredLevel[level];
  const available = !maxed && prereqsMet && heroLevelMet;
  const locked = level === 0 && !available;
  return { level, maxed, available, learned: level > 0, locked, prereqsMet, heroLevelMet };
}

const STAT_LABELS = {
  hpMult: 'Max HP', mpMult: 'Max MP', defAdd: 'DEF', speedMult: 'Speed', comboMult: 'Combo DMG',
  critChance: 'Crit Chance', regenMult: 'Regen', mpRegenMult: 'MP Regen',
};
const PERCENT_STATS = new Set(['hpMult', 'mpMult', 'speedMult', 'comboMult', 'critChance', 'regenMult', 'mpRegenMult']);

function formatPassiveLine(stat, value) {
  const label = STAT_LABELS[stat] || stat;
  if (stat === 'defAdd') return `+${value} ${label}`;
  return `+${Math.round(value * (PERCENT_STATS.has(stat) ? 100 : 1))}% ${label}`;
}

/** One-line summary of what the next skill level grants (or the current level if maxed). */
export function describeNextLevel(skillId, level) {
  const def = SKILL_DEFINITIONS[skillId];
  const maxed = level >= def.maxLevel;
  const target = maxed ? level : level + 1;
  const prefix = maxed ? `Lv${target} (MAX)` : `Lv${target}`;
  if (def.type === 'passive') {
    const parts = Object.entries(def.passive).map(([stat, arr]) => formatPassiveLine(stat, arr[target - 1]));
    return `${prefix}: ${parts.join(', ')}`;
  }
  const mp = getSkillValue(skillId, 'mpCost', target);
  const cd = (getSkillValue(skillId, 'cooldownMs', target) / 1000).toFixed(1);
  const extra = def.effect.kind === 'heal' ? `${getSkillValue(skillId, 'healPercent', target)}% HP`
    : def.effect.kind === 'buff' ? `x${getSkillValue(skillId, 'atkMult', target).toFixed(2)} ATK for ${(def.effect.durationMs / 1000).toFixed(0)}s`
    : def.damageMult ? `${Math.round(getSkillValue(skillId, 'damageMult', target) * 100)}% ATK` : '';
  return `${prefix}: ${extra}   ${mp} MP   ${cd}s CD`;
}

/** Requirement text for the info box: hero level + prerequisite nodes. */
export function describeRequirement(player, skillId) {
  const def = SKILL_DEFINITIONS[skillId];
  const level = getSkillLevel(player, skillId);
  if (level >= def.maxLevel) return 'Mastered';
  const parts = [`Hero Lv ${def.requiredLevel[level]}`];
  for (const req of def.requires || []) parts.push(`${SKILL_DEFINITIONS[req.skill].name} Lv ${req.level}`);
  return `Requires: ${parts.join(', ')}`;
}
