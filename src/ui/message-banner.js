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
    if (this.lifeMs <= 0) return;
    const fadeIn = Math.min(1, (this.maxLifeMs - this.lifeMs) / 200);
    const fadeOut = Math.min(1, this.lifeMs / 400);
    ctx.globalAlpha = Math.min(fadeIn, fadeOut);
    const y = CANVAS_HEIGHT * 0.28;
    ctx.fillStyle = 'rgba(0,0,0,0.55)';
    ctx.fillRect(0, y - 14, CANVAS_WIDTH, this.subtext ? 78 : 52);
    drawText(ctx, this.text, CANVAS_WIDTH / 2, y, { size: 30, align: 'center', color: this.color });
    if (this.subtext) drawText(ctx, this.subtext, CANVAS_WIDTH / 2, y + 40, { size: 15, align: 'center', color: '#d0d0e0' });
    ctx.globalAlpha = 1;
  }
}
