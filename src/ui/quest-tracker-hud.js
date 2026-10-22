// Compact top-right tracker listing up to three active quests with progress.
import { CANVAS_WIDTH } from '../core/constants.js';
import { drawText } from './pixel-text.js';
import { QUEST_DEFINITIONS } from '../data/quest-definitions.js';
import { progressText, isReadyToTurnIn } from '../systems/quest-system.js';

const MAX_SHOWN = 3;

export function drawQuestTracker(ctx, questState, topY = 70) {
  const ids = Object.keys(questState.active).slice(0, MAX_SHOWN);
  if (ids.length === 0) return;
  const x = CANVAS_WIDTH - 20;
  ctx.fillStyle = 'rgba(0,0,0,0.45)';
  ctx.fillRect(x - 250, topY - 6, 250, 22 + ids.length * 20);
  drawText(ctx, 'QUESTS  [Q]', x, topY, { size: 12, align: 'right', color: '#ffe680' });
  ids.forEach((id, i) => {
    const ready = isReadyToTurnIn(questState, id);
    const label = `${QUEST_DEFINITIONS[id].title}  ${ready ? 'DONE' : progressText(questState, id)}`;
    drawText(ctx, label, x, topY + 18 + i * 20, { size: 12, align: 'right', color: ready ? '#8fff8f' : '#e8e8f0', bold: false });
  });
}
