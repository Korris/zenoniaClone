// Talking to NPCs: finds the nearest NPC, builds the dialog for their current quest state,
// and applies the outcome (accept / turn in) when the dialog closes.
import { QUEST_DEFINITIONS } from '../data/quest-definitions.js';
import { nextQuestForNpc, acceptQuest, turnInQuest, npcMarker } from '../systems/quest-system.js';

const TALK_RANGE = 26;

/** Nearest NPC within talking range of the hero, or null. */
export function findNearbyNpc(world) {
  const { player } = world;
  let best = null;
  let bestDist = TALK_RANGE;
  for (const npc of world.npcs) {
    const d = Math.hypot(npc.x - player.x, npc.y - player.y);
    if (d < bestDist) {
      best = npc;
      bestDist = d;
    }
  }
  return best;
}

/** Marker kind per NPC id for the renderer/minimap. */
export function computeNpcMarkers(world, questState) {
  const markers = {};
  for (const npc of world.npcs) {
    const marker = npcMarker(questState, world.player, npc.id);
    if (marker) markers[npc.id] = marker;
  }
  return markers;
}

/**
 * Open the right conversation for this NPC. Returns the payload the dialog carries so the
 * controller can resolve it in applyDialogOutcome().
 */
export function openNpcDialog(dialog, npc, player, questState, portrait = null) {
  npc.faceTowards(player.x, player.y);
  const next = nextQuestForNpc(questState, player, npc.id);
  if (!next) {
    dialog.open({ speaker: npc.name, lines: npc.def.lines, payload: null, portrait });
    return;
  }
  const quest = QUEST_DEFINITIONS[next.questId];
  if (next.kind === 'offer') {
    const lines = [...quest.dialog.offer, `[${quest.title}] ${quest.description} Reward: ${quest.rewards.exp} EXP, ${quest.rewards.gold} gold.`];
    dialog.open({ speaker: npc.name, lines, choices: ['Accept', 'Decline'], payload: { kind: 'offer', questId: quest.id }, portrait });
  } else if (next.kind === 'turnIn') {
    dialog.open({ speaker: npc.name, lines: quest.dialog.complete, payload: { kind: 'turnIn', questId: quest.id }, portrait });
  } else {
    dialog.open({ speaker: npc.name, lines: quest.dialog.progress, payload: null, portrait });
  }
}

/**
 * Apply what happened when the dialog closed.
 * @returns {{type:'accepted'|'turnedIn', questId:string, rewards?:object}|null}
 */
export function applyDialogOutcome(payload, choice, player, questState) {
  if (!payload) return null;
  if (payload.kind === 'offer' && choice === 'Accept') {
    return acceptQuest(questState, player, payload.questId) ? { type: 'accepted', questId: payload.questId } : null;
  }
  if (payload.kind === 'turnIn') {
    const rewards = turnInQuest(questState, player, payload.questId);
    return rewards ? { type: 'turnedIn', questId: payload.questId, rewards } : null;
  }
  return null;
}
