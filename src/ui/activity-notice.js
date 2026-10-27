import { CANVAS_WIDTH } from '../core/constants.js';
import { drawText } from './pixel-text.js';
import { itemSprite } from '../graphics/weapon-icons.js';
import { drawPotion } from '../graphics/potion-renderer.js';

export const ACTIVITY_NOTICE = Object.freeze({ x: CANVAS_WIDTH - 240, y: 158, width: 224, height: 58 });

function panel(ctx, x, y, w, h, fill, border) {
  ctx.beginPath(); ctx.roundRect(x, y, w, h, 4);
  ctx.fillStyle = fill; ctx.fill();
  ctx.strokeStyle = border; ctx.lineWidth = 1; ctx.stroke();
}

export function drawActivityNotice(ctx, notice, registry, classId) {
  if (!notice || notice.lifeMs <= 0) return;
  const { x, y, width, height } = ACTIVITY_NOTICE;
  const quest = notice.mode === 'quest';
  const accent = notice.mode === 'picked' ? '#85dfa3' : quest ? '#efca79' : notice.mode === 'error' ? '#ff8c9c' : '#70cfff';
  const heading = { preview: 'NEARBY', picked: 'PICKED UP', error: 'BAG FULL', quest: notice.ready ? 'QUEST READY' : 'QUEST PROGRESS' }[notice.mode];
  ctx.save();
  ctx.globalAlpha *= Math.min(1, (notice.maxLifeMs - notice.lifeMs) / 140, notice.lifeMs / 220);
  panel(ctx, x, y, width, height, '#101e2dee', '#4b5a6a');
  ctx.fillStyle = accent; ctx.fillRect(x, y + 4, 3, height - 8);
  panel(ctx, x + 12, y + 10, 36, 36, '#0d1928', '#7497af');
  const drop = notice.drop;
  if (drop?.kind === 'equipment') {
    const sprite = itemSprite(registry, drop.item, classId, true);
    if (sprite) {
      const scale = 30 / Math.max(sprite.width, sprite.height);
      ctx.imageSmoothingEnabled = true;
      ctx.drawImage(sprite, x + 30 - sprite.width * scale / 2, y + 28 - sprite.height * scale / 2, sprite.width * scale, sprite.height * scale);
    }
  } else if (drop?.kind === 'hpPotion' || drop?.kind === 'mpPotion') {
    drawPotion(ctx, drop.kind, x + 30, y + 28, 27);
  } else if (drop?.kind === 'coin') {
    ctx.beginPath(); ctx.arc(x + 30, y + 28, 11, 0, Math.PI * 2);
    ctx.fillStyle = '#e7b652'; ctx.fill();
    drawText(ctx, 'G', x + 30, y + 20, { size: 14, color: '#654219', align: 'center', shadow: false });
  } else {
    // Small parchment icon in the established gold/navy palette.
    panel(ctx, x + 21, y + 17, 18, 24, '#c9a36a', '#f0d396');
    ctx.fillStyle = '#715336';
    for (let i = 0; i < 3; i++) ctx.fillRect(x + 25, y + 23 + i * 5, 10, 1);
  }
  const textX = x + 59;
  ctx.beginPath(); ctx.rect(textX, y + 6, width - 68, height - 10); ctx.clip();
  drawText(ctx, heading || 'NOTICE', textX, y + 9, { size: 9, color: accent, shadow: false });
  drawText(ctx, notice.text, textX, y + 25, { size: quest ? 12 : 13, color: notice.color || '#eef5fa', shadow: false });
  if (quest && notice.total > 0) {
    const ratio = Math.max(0, Math.min(1, notice.current / notice.total));
    ctx.fillStyle = '#304659'; ctx.fillRect(textX, y + 46, 105, 3);
    ctx.fillStyle = accent; ctx.fillRect(textX, y + 46, 105 * ratio, 3);
    drawText(ctx, `${notice.current}/${notice.total}`, x + width - 10, y + 40, { size: 10, color: accent, align: 'right', shadow: false });
  }
  ctx.restore();
}
