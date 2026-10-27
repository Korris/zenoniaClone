// Bottom dialog box with speaker name, typewriter text and optional Accept/Decline choice.
// Enter/E advances; the last page may present choices resolved via `result.choice`.
import { CANVAS_WIDTH, CANVAS_HEIGHT } from '../core/constants.js';
import { drawText, drawPanel, drawScaledSprite } from './pixel-text.js';

const BOX_H = 150;
const BOX_Y = CANVAS_HEIGHT - BOX_H - 120;
const BOX_X = 60;
const BOX_W = CANVAS_WIDTH - 120;
const CHARS_PER_SECOND = 60;

export class DialogBox {
  constructor() {
    this.speaker = '';
    this.pages = [];
    this.pageIndex = 0;
    this.choices = null;
    this.selected = 0;
    this.revealed = 0;
    this.portrait = null;
    this.payload = null; // opaque data handed back when the dialog resolves
  }

  /** @param {{speaker:string, lines:string[], choices?:string[], portrait?:HTMLCanvasElement, payload?:any}} opts */
  open(opts) {
    this.speaker = opts.speaker;
    this.pages = opts.lines.length ? opts.lines : ['...'];
    this.choices = opts.choices || null;
    this.portrait = opts.portrait || null;
    this.payload = opts.payload ?? null;
    this.pageIndex = 0;
    this.selected = 0;
    this.revealed = 0;
  }

  get isLastPage() {
    return this.pageIndex >= this.pages.length - 1;
  }

  get fullyRevealed() {
    return this.revealed >= this.pages[this.pageIndex].length;
  }

  update(dt) {
    this.revealed += (CHARS_PER_SECOND * dt) / 1000;
  }

  /** @returns {{closed:boolean, choice:string|null}} */
  handleInput(input) {
    const result = { closed: false, choice: null };
    const showingChoices = this.isLastPage && this.choices && this.fullyRevealed;
    if (showingChoices) {
      if (input.wasPressed('moveLeft') || input.wasPressed('moveUp')) this.selected = (this.selected + this.choices.length - 1) % this.choices.length;
      if (input.wasPressed('moveRight') || input.wasPressed('moveDown')) this.selected = (this.selected + 1) % this.choices.length;
    }
    if (input.wasPressed('pause')) {
      result.closed = true;
      result.choice = this.choices ? this.choices[this.choices.length - 1] : null; // Esc = decline
      return result;
    }
    if (!(input.wasPressed('interact') || input.wasPressed('confirm') || input.takeClicks().length)) return result;
    if (!this.fullyRevealed) {
      this.revealed = Infinity;
      return result;
    }
    if (!this.isLastPage) {
      this.pageIndex++;
      this.revealed = 0;
      return result;
    }
    result.closed = true;
    result.choice = this.choices ? this.choices[this.selected] : null;
    return result;
  }

  render(ctx) {
    drawPanel(ctx, BOX_X, BOX_Y, BOX_W, BOX_H, { fill: 'rgba(12,10,28,0.92)', border: '#d9b45a' });
    let textX = BOX_X + 24;
    if (this.portrait) {
      // 38x38 pixel faceset drawn at 3x inside a framed box; an HD portrait (pixelsPerUnit > 1,
      // 152px) is smooth-fitted into the same 114x114 area instead.
      drawPanel(ctx, BOX_X + 14, BOX_Y + 14, 122, 122, { fill: '#1b1b2a', border: '#8c8cb0' });
      if ((this.portrait.pixelsPerUnit || 1) > 1) {
        ctx.imageSmoothingEnabled = true;
        ctx.drawImage(this.portrait, BOX_X + 18, BOX_Y + 18, 114, 114);
        ctx.imageSmoothingEnabled = false;
      } else {
        drawScaledSprite(ctx, this.portrait, BOX_X + 18, BOX_Y + 18, 3);
      }
      textX = BOX_X + 150;
    }
    drawText(ctx, this.speaker, textX, BOX_Y + 14, { size: 16, color: '#ffe680' });
    const page = this.pages[this.pageIndex];
    const shown = page.slice(0, Math.floor(this.revealed));
    this.wrap(ctx, shown, textX, BOX_Y + 44, BOX_W - (textX - BOX_X) - 24);

    if (this.isLastPage && this.choices && this.fullyRevealed) {
      this.choices.forEach((label, i) => {
        const x = BOX_X + BOX_W - 150 - (this.choices.length - 1 - i) * 140;
        const selected = i === this.selected;
        drawText(ctx, `${selected ? '> ' : ''}${label}`, x, BOX_Y + BOX_H - 34, { size: 15, color: selected ? '#8fff8f' : '#b8b8cc' });
      });
    } else if (this.fullyRevealed) {
      const blink = Math.floor(performance.now() / 400) % 2 === 0;
      if (blink) drawText(ctx, this.isLastPage ? '[E] Close' : '[E] Next', BOX_X + BOX_W - 24, BOX_Y + BOX_H - 30, { size: 13, align: 'right', color: '#9a9ab0' });
    }
  }

  wrap(ctx, text, x, y, maxWidth) {
    ctx.font = 'bold 15px "Segoe UI", Arial, sans-serif';
    const words = text.split(' ');
    let line = '';
    let lineY = y;
    for (const word of words) {
      const candidate = line ? `${line} ${word}` : word;
      if (ctx.measureText(candidate).width > maxWidth && line) {
        drawText(ctx, line, x, lineY, { size: 15, color: '#f0f0f8' });
        line = word;
        lineY += 22;
      } else line = candidate;
    }
    if (line) drawText(ctx, line, x, lineY, { size: 15, color: '#f0f0f8' });
  }
}
