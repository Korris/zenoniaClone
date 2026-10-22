// Quest state machine. Pure (no DOM) -> unit-testable.
// state shape: { active: {questId: {progress:number}}, completed: string[] }
import { QUEST_DEFINITIONS } from '../data/quest-definitions.js';
import { NPC_DEFINITIONS } from '../data/npc-definitions.js';

export function createQuestState() {
  return { active: {}, completed: [] };
}

/** Rebuild a valid state from untrusted (saved) data. */
export function sanitizeQuestState(raw) {
  const state = createQuestState();
  if (!raw || typeof raw !== 'object') return state;
  for (const id of raw.completed || []) if (QUEST_DEFINITIONS[id] && !state.completed.includes(id)) state.completed.push(id);
  for (const [id, entry] of Object.entries(raw.active || {})) {
    if (!QUEST_DEFINITIONS[id] || state.completed.includes(id)) continue; // completed wins over a stale active entry
    state.active[id] = { progress: Math.max(0, Math.floor(Number(entry?.progress) || 0)) };
  }
  return state;
}

export function objectiveTarget(questId) {
  const obj = QUEST_DEFINITIONS[questId].objective;
  return obj.type === 'kill' ? obj.count : 1;
}

export function isReadyToTurnIn(state, questId) {
  const entry = state.active[questId];
  return Boolean(entry) && entry.progress >= objectiveTarget(questId);
}

/** 'completed' | 'ready' | 'active' | 'available' | 'locked' */
export function questStatus(state, player, questId) {
  const def = QUEST_DEFINITIONS[questId];
  if (!def) return 'locked';
  if (state.completed.includes(questId)) return 'completed';
  if (state.active[questId]) return isReadyToTurnIn(state, questId) ? 'ready' : 'active';
  if (def.prerequisite && !state.completed.includes(def.prerequisite)) return 'locked';
  if (player.level < def.requiredLevel) return 'locked';
  return 'available';
}

export function acceptQuest(state, player, questId) {
  if (questStatus(state, player, questId) !== 'available') return false;
  state.active[questId] = { progress: 0 };
  return true;
}

/** Advance every active kill quest for this monster type. Returns ids that progressed. */
export function recordKill(state, monsterType) {
  const touched = [];
  for (const [id, entry] of Object.entries(state.active)) {
    const obj = QUEST_DEFINITIONS[id].objective;
    if (obj.type !== 'kill' || obj.monster !== monsterType || entry.progress >= obj.count) continue;
    entry.progress += 1;
    touched.push(id);
  }
  return touched;
}

/** Complete every active "reach" quest for this map. Returns ids that progressed. */
export function recordMapEnter(state, mapId) {
  const touched = [];
  for (const [id, entry] of Object.entries(state.active)) {
    const obj = QUEST_DEFINITIONS[id].objective;
    if (obj.type !== 'reach' || obj.map !== mapId || entry.progress >= 1) continue;
    entry.progress = 1;
    touched.push(id);
  }
  return touched;
}

/** Hand in a ready quest: grants rewards via the player object. Returns rewards or null. */
export function turnInQuest(state, player, questId) {
  if (!isReadyToTurnIn(state, questId)) return null;
  const { rewards } = QUEST_DEFINITIONS[questId];
  delete state.active[questId];
  state.completed.push(questId);
  const levelsGained = player.gainExp(rewards.exp);
  player.gold += rewards.gold;
  return { ...rewards, levelsGained };
}

/** What this NPC wants to talk about right now (turn-ins first, then offers, then progress). */
export function nextQuestForNpc(state, player, npcId) {
  const npc = NPC_DEFINITIONS[npcId];
  if (!npc) return null;
  const statuses = npc.quests.map((id) => ({ id, status: questStatus(state, player, id) }));
  const pick = (status, kind) => {
    const found = statuses.find((q) => q.status === status);
    return found ? { questId: found.id, kind } : null;
  };
  return pick('ready', 'turnIn') || pick('available', 'offer') || pick('active', 'progress');
}

/** Marker sprite key for the NPC: 'turnIn' (?), 'available' (!), or null. */
export function npcMarker(state, player, npcId) {
  const next = nextQuestForNpc(state, player, npcId);
  if (!next) return null;
  return next.kind === 'turnIn' ? 'turnIn' : next.kind === 'offer' ? 'available' : null;
}

/** Short progress label for HUD/log, e.g. "3/5" or "Reach Dark Cave". */
export function progressText(state, questId) {
  const def = QUEST_DEFINITIONS[questId];
  const entry = state.active[questId];
  if (def.objective.type === 'kill') return `${Math.min(entry?.progress || 0, def.objective.count)}/${def.objective.count}`;
  return entry && entry.progress >= 1 ? 'Done' : 'Travel there';
}
