// Centered announcement text (map name, level up, locked portal) that fades out.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { drawText } from './pixel-text.js';

export class MessageBanner {
  constructor() {
    this.text = '';
    this.subtext = '';
    this.color = '#ffffff';
    this.lifeMs = 0;
    this.maxLifeMs = 1;
  }

  show(text, subtext = '', durationMs = 2200, color = '#ffffff') {
    this.text = text;
    this.subtext = subtext;
    this.color = color;
    this.lifeMs = durationMs;
    this.maxLifeMs = durationMs;
  }

  update(dt) {
    if (this.lifeMs > 0) this.lifeMs -= dt;
  }

  render(ctx) {
    drawMessageBanner(ctx, this);
  }
}

/** Loot shares announcement styling in a smaller card below the level/map banner. */
export function drawMessageBanner(ctx, message, compact = false) {
    if (!message || message.lifeMs <= 0) return;
    const fadeIn = Math.min(1, (message.maxLifeMs - message.lifeMs) / 200);
    const fadeOut = Math.min(1, message.lifeMs / 400);
    ctx.save();
    ctx.globalAlpha *= Math.min(fadeIn, fadeOut);
    const y = CANVAS_HEIGHT * (compact ? 0.43 : 0.28);
    const width = compact ? 420 : CANVAS_WIDTH;
    ctx.fillStyle = 'rgba(0,0,0,0.55)';
    ctx.fillRect((CANVAS_WIDTH - width) / 2, y - (compact ? 10 : 14), width, compact ? 56 : message.subtext ? 78 : 52);
    if (compact) {
      ctx.beginPath(); ctx.rect((CANVAS_WIDTH - width) / 2 + 8, y - 10, width - 16, 56); ctx.clip();
    }
    drawText(ctx, message.text, CANVAS_WIDTH / 2, y, { size: compact ? 18 : 30, align: 'center', color: message.color });
    if (message.subtext) drawText(ctx, message.subtext, CANVAS_WIDTH / 2, y + (compact ? 27 : 40), { size: compact ? 12 : 15, align: 'center', color: '#d0d0e0' });
    ctx.restore();
}
