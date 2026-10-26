// New Game -> "Choose your hero": one card per playable class with an animated idle preview,
// tagline, attack style and base-stat bars. Left/Right (or click) select, Enter/J confirm,
// Esc back to the title.
import { CANVAS_WIDTH, CANVAS_HEIGHT, PIXEL_SCALE } from '../core/constants.js';
import { drawText, drawPanel, UI_FONT } from './pixel-text.js';
import { HERO_FRAME_SIZE } from '../graphics/sprite-registry.js';
import { CLASS_DEFINITIONS, CLASS_IDS } from '../data/class-definitions.js';

const CARD_W = 280;
const CARD_H = 330;
const CARD_GAP = 20;
const CARD_Y = 150;
const IDLE_FRAME_MS = 200;
const STAT_BARS = [
  { key: 'maxHp', label: 'HP', color: '#e85050' },
  { key: 'maxMp', label: 'MP', color: '#4f8ff0' },
  { key: 'atk', label: 'ATK', color: '#f0b040' },
  { key: 'def', label: 'DEF', color: '#9aa8b8' },
];

/** Word-wrap `text` into lines no wider than `maxWidth` at the ctx's current font. */
function wrapLines(ctx, text, maxWidth) {
  const lines = [];
  let line = '';
  for (const word of text.split(' ')) {
    const next = line ? `${line} ${word}` : word;
    if (line && ctx.measureText(next).width > maxWidth) {
      lines.push(line);
      line = word;
    } else {
      line = next;
    }
  }
  if (line) lines.push(line);
  return lines;
}

export class ClassSelectScreen {
  constructor() {
    this.selected = 0;
  }

  cardX(index) {
    const total = CLASS_IDS.length * CARD_W + (CLASS_IDS.length - 1) * CARD_GAP;
    return (CANVAS_WIDTH - total) / 2 + index * (CARD_W + CARD_GAP);
  }

  /** @returns {{classId?:string, back?:boolean}} what the player decided this frame (or {}) */
  handleInput(input) {
    const count = CLASS_IDS.length;
    if (input.wasPressed('moveLeft')) this.selected = (this.selected + count - 1) % count;
    if (input.wasPressed('moveRight')) this.selected = (this.selected + 1) % count;
    for (const click of input.takeClicks()) {
      const index = CLASS_IDS.findIndex((_, i) => click.x >= this.cardX(i) && click.x <= this.cardX(i) + CARD_W
        && click.y >= CARD_Y && click.y <= CARD_Y + CARD_H);
      if (index === -1) continue;
      if (index === this.selected) return { classId: CLASS_IDS[index] };
      this.selected = index;
    }
    if (input.wasPressed('confirm')) return { classId: CLASS_IDS[this.selected] };
    if (input.wasPressed('pause')) return { back: true };
    return {};
  }

  render(ctx, registry, nowMs) {
    ctx.fillStyle = '#0b0b16';
    ctx.fillRect(0, 0, CANVAS_WIDTH, CANVAS_HEIGHT);
    drawText(ctx, 'CHOOSE YOUR HERO', CANVAS_WIDTH / 2, 50, { size: 40, align: 'center', color: '#ffe680' });
    drawText(ctx, 'Left / Right to choose   Enter to start   Esc to go back', CANVAS_WIDTH / 2, 100, { size: 13, align: 'center', color: '#9a9ab0' });
    const maxStat = Object.fromEntries(STAT_BARS.map(({ key }) => [key, Math.max(...CLASS_IDS.map((id) => CLASS_DEFINITIONS[id].baseStats[key]))]));
    CLASS_IDS.forEach((classId, i) => this.renderCard(ctx, registry, classId, i, maxStat, nowMs));
  }

  renderCard(ctx, registry, classId, index, maxStat, nowMs) {
    const def = CLASS_DEFINITIONS[classId];
    const x = this.cardX(index);
    const selected = index === this.selected;
    drawPanel(ctx, x, CARD_Y, CARD_W, CARD_H, selected
      ? { fill: 'rgba(60,50,20,0.92)', border: '#ffe680' }
      : { fill: 'rgba(16,16,30,0.92)', border: '#55557a' });

    // Animated idle preview, drawn at the same world-unit size as in game (x PIXEL_SCALE) -- the
    // selected class faces the viewer, the other one looks toward it.
    const set = registry.heroes?.[classId] || registry.hero;
    const frames = set.down.idle;
    const frame = frames[Math.floor(nowMs / IDLE_FRAME_MS) % frames.length];
    const size = HERO_FRAME_SIZE * PIXEL_SCALE * 1.3;
    ctx.imageSmoothingEnabled = (frame.pixelsPerUnit || 1) > 1;
    ctx.globalAlpha = selected ? 1 : 0.55;
    ctx.drawImage(frame, x + CARD_W / 2 - size / 2, CARD_Y + 14, size, size);
    ctx.globalAlpha = 1;
    ctx.imageSmoothingEnabled = false;

    let y = CARD_Y + 14 + size + 4;
    drawText(ctx, def.name, x + CARD_W / 2, y, { size: 24, align: 'center', color: selected ? '#ffffff' : '#b8b8cc' });
    y += 32;
    ctx.font = `bold 12px ${UI_FONT}`; // same font drawText uses, so wrapping measures true widths
    for (const line of wrapLines(ctx, def.tagline, CARD_W - 32)) {
      drawText(ctx, line, x + CARD_W / 2, y, { size: 12, align: 'center', color: '#d0d0e0' });
      y += 16;
    }
    drawText(ctx, def.styleLabel, x + CARD_W / 2, y + 2, { size: 12, align: 'center', color: '#ffd27a' });
    y += 24;
    for (const { key, label, color } of STAT_BARS) {
      const value = def.baseStats[key];
      drawText(ctx, label, x + 24, y, { size: 12, color: '#c8c8dc' });
      ctx.fillStyle = 'rgba(255,255,255,0.12)';
      ctx.fillRect(x + 70, y + 2, CARD_W - 130, 9);
      ctx.fillStyle = color;
      ctx.fillRect(x + 70, y + 2, (CARD_W - 130) * (value / maxStat[key]), 9);
      drawText(ctx, String(value), x + CARD_W - 24, y, { size: 12, align: 'right', color: '#ffffff' });
      y += 17;
    }
  }
}
