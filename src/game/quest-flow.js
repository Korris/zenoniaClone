// Glue between gameplay events and the quest system: progress feedback, banners, rewards.
import { QUEST_DEFINITIONS } from '../data/quest-definitions.js';
import { recordKill, isReadyToTurnIn, progressText, objectiveTarget } from '../systems/quest-system.js';
import { NPC_DEFINITIONS } from '../data/npc-definitions.js';
import { showActivityNotice } from '../systems/activity-notice-system.js';

/** A monster died: advance kill quests and show progress / completion feedback. */
export function onMonsterKilledForQuests(session, monsterType) {
  const { world, quests } = session;
  for (const questId of recordKill(quests, monsterType)) {
    const def = QUEST_DEFINITIONS[questId];
    const ready = isReadyToTurnIn(quests, questId);
    showActivityNotice(world, { mode: 'quest', questId, text: def.title,
      subtext: ready ? `Return to ${NPC_DEFINITIONS[def.giver].name}` : progressText(quests, questId),
      current: quests.active[questId].progress, total: objectiveTarget(questId), ready,
      color: ready ? '#8fff8f' : '#ffe680' }, 2000);
  }
}

/** Reach quests completed by entering a map (keeps the map name as the banner title). */
export function onMapReachedForQuests(session, questIds) {
  for (const questId of questIds) {
    const def = QUEST_DEFINITIONS[questId];
    session.banner.show(session.world.mapDef.name, `Quest complete: ${def.title}. Return to ${NPC_DEFINITIONS[def.giver].name}`, 3000, '#8fff8f');
  }
}

/** Dialog closed with an accept / turn-in outcome. */
export function onDialogOutcomeForQuests(session, outcome) {
  if (!outcome) return;
  const def = QUEST_DEFINITIONS[outcome.questId];
  const { world, player } = session;
  if (outcome.type === 'accepted') {
    session.banner.show('Quest accepted', def.title, 1800, '#ffe680');
    // A boss killed before the quest was accepted never respawns: count it immediately
    const obj = def.objective;
    if (obj.type === 'kill' && world.bossDefeated && world.mapDef.boss?.type === obj.monster) {
      session.quests.active[outcome.questId].progress = obj.count;
    }
  } else if (outcome.type === 'turnedIn') {
    const { exp, gold, levelsGained } = outcome.rewards;
    session.banner.show('Quest complete!', `${def.title}  +${exp} EXP  +${gold} gold`, 2600, '#8fff8f');
    world.addText(player.x, player.y - 30, `+${exp} EXP`, '#f2c14e', { lifeMs: 1200 });
    world.addText(player.x, player.y - 44, `+${gold} G`, '#ffd84a', { lifeMs: 1200 });
    if (levelsGained > 0) world.addText(player.x, player.y - 60, 'LEVEL UP!', '#8fff8f', { lifeMs: 1400, big: true, vy: -18 });
  }
  session.save(false);
}
