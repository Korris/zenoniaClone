// Quest log overlay (Q): active quests with progress, then completed ones.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { drawText, drawPanel, drawOverlay } from './pixel-text.js';
import { QUEST_DEFINITIONS } from '../data/quest-definitions.js';
import { NPC_DEFINITIONS } from '../data/npc-definitions.js';
import { progressText, isReadyToTurnIn } from '../systems/quest-system.js';

const PANEL_W = 640;
const PANEL_H = 460;
const PANEL_X = (CANVAS_WIDTH - PANEL_W) / 2;
const PANEL_Y = (CANVAS_HEIGHT - PANEL_H) / 2;

export class QuestLogPanel {
  /** @returns {{close:boolean}} */
  handleInput(input) {
    return { close: input.wasPressed('questLog') || input.wasPressed('pause') || input.wasPressed('skillPanel') };
  }

  render(ctx, questState) {
    drawOverlay(ctx, 0.6);
    drawPanel(ctx, PANEL_X, PANEL_Y, PANEL_W, PANEL_H);
    drawText(ctx, 'QUEST LOG', PANEL_X + 24, PANEL_Y + 18, { size: 24, color: '#ffe680' });
    drawText(ctx, 'Q / Esc close', PANEL_X + PANEL_W - 24, PANEL_Y + 24, { size: 12, align: 'right', color: '#9a9ab0' });

    let y = PANEL_Y + 60;
    const activeIds = Object.keys(questState.active);
    drawText(ctx, `Active (${activeIds.length})`, PANEL_X + 24, y, { size: 15, color: '#8fc3ff' });
    y += 24;
    if (activeIds.length === 0) {
      drawText(ctx, 'No active quests. Look for NPCs with a "!" above them.', PANEL_X + 32, y, { size: 13, color: '#b8b8cc', bold: false });
      y += 24;
    }
    for (const id of activeIds) {
      y = this.renderEntry(ctx, questState, id, y, true);
      if (y > PANEL_Y + PANEL_H - 90) break;
    }

    y += 10;
    drawText(ctx, `Completed (${questState.completed.length})`, PANEL_X + 24, y, { size: 15, color: '#8fff8f' });
    y += 24;
    for (const id of questState.completed) {
      if (y > PANEL_Y + PANEL_H - 40) break;
      drawText(ctx, `- ${QUEST_DEFINITIONS[id].title}`, PANEL_X + 32, y, { size: 13, color: '#9a9ab0', bold: false });
      y += 18;
    }
  }

  renderEntry(ctx, questState, id, y, active) {
    const def = QUEST_DEFINITIONS[id];
    const ready = isReadyToTurnIn(questState, id);
    const giver = NPC_DEFINITIONS[def.giver]?.name || def.giver;
    drawText(ctx, def.title, PANEL_X + 32, y, { size: 15, color: ready ? '#8fff8f' : '#ffffff' });
    drawText(ctx, ready ? `Return to ${giver}` : progressText(questState, id), PANEL_X + PANEL_W - 32, y, { size: 14, align: 'right', color: ready ? '#8fff8f' : '#ffe680' });
    drawText(ctx, def.description, PANEL_X + 32, y + 20, { size: 12, color: '#b8b8cc', bold: false });
    drawText(ctx, `Reward: ${def.rewards.exp} EXP, ${def.rewards.gold} gold`, PANEL_X + 32, y + 36, { size: 11, color: '#9a9ab0', bold: false });
    void active;
    return y + 58;
  }
}
