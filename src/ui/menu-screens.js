// Title, pause, death and victory screens. A generic vertical menu handles selection.
import { CANVAS_WIDTH, CANVAS_HEIGHT, PIXEL_SCALE } from '../core/constants.js';
import { drawText, drawOverlay, drawPanel } from './pixel-text.js';
import { HERO_FRAME_SIZE } from '../graphics/sprite-registry.js';

export class MenuScreen {
  /** @param {{title:string, subtitle?:string, options:string[], hint?:string, color?:string}} config */
  constructor(config) {
    this.config = config;
    this.selected = 0;
  }

  setOptions(options) {
    this.config.options = options;
    this.selected = Math.min(this.selected, options.length - 1);
  }

  /** @returns {string|null} chosen option label or null */
  handleInput(input) {
    const count = this.config.options.length;
    if (input.wasPressed('moveUp')) this.selected = (this.selected + count - 1) % count;
    if (input.wasPressed('moveDown')) this.selected = (this.selected + 1) % count;
    for (const click of input.takeClicks()) {
      const row = Math.floor((click.y - this.optionsTop()) / 40);
      if (row >= 0 && row < count) {
        if (this.selected === row) return this.config.options[row];
        this.selected = row;
      }
    }
    if (input.wasPressed('confirm')) return this.config.options[this.selected];
    return null;
  }

  optionsTop() {
    return CANVAS_HEIGHT / 2 + 10;
  }

  render(ctx, registry, opts = {}) {
    if (opts.dim !== false) drawOverlay(ctx, opts.dimAlpha ?? 0.7);
    const { title, subtitle, options, hint, color = '#ffe680' } = this.config;
    drawText(ctx, title, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 150, { size: 52, align: 'center', color });
    if (subtitle) drawText(ctx, subtitle, CANVAS_WIDTH / 2, CANVAS_HEIGHT / 2 - 100, { size: 16, align: 'center', color: '#d0d0e0' });
    if (registry && opts.showHero) {
      const hero = registry.hero.down.idle[0]; // HD frame if that sheet loaded, else the pixel-art one
      // Dest size is world-unit based (HERO_FRAME_SIZE * PIXEL_SCALE), not source-resolution based,
      // so the title hero is the same screen size whether it's the 96px HD frame or the 24px pixel one.
      const destSize = HERO_FRAME_SIZE * PIXEL_SCALE;
      ctx.imageSmoothingEnabled = (hero.pixelsPerUnit || 1) > 1;
      ctx.drawImage(hero, CANVAS_WIDTH / 2 - destSize / 2, CANVAS_HEIGHT / 2 - 100, destSize, destSize);
      ctx.imageSmoothingEnabled = false;
    }
    const top = this.optionsTop();
    options.forEach((label, i) => {
      const selected = i === this.selected;
      if (selected) drawPanel(ctx, CANVAS_WIDTH / 2 - 160, top + i * 40 - 4, 320, 34, { fill: 'rgba(255,230,128,0.15)', border: '#ffe680' });
      drawText(ctx, `${selected ? '> ' : ''}${label}`, CANVAS_WIDTH / 2, top + i * 40 + 4, { size: 20, align: 'center', color: selected ? '#ffffff' : '#b8b8cc' });
    });
    if (hint) drawText(ctx, hint, CANVAS_WIDTH / 2, top + options.length * 40 + 16, { size: 12, align: 'center', color: '#9a9ab0' });
  }
}

export function createTitleMenu(hasSave) {
  return new MenuScreen({
    title: 'ZENONIA WEB',
    subtitle: 'A chibi hack & slash adventure',
    options: hasSave ? ['Continue', 'New Game'] : ['New Game'],
    hint: 'WASD/Arrows move  J/Space attack  K/L/I skills  Tab skills  Esc pause',
  });
}

export function createPauseMenu() {
  return new MenuScreen({ title: 'PAUSED', options: ['Resume', 'Save Game', 'Quit to Title'], hint: 'Esc to resume' });
}

export function createVictoryMenu(player) {
  return new MenuScreen({
    title: 'VICTORY!',
    subtitle: `The Demon Cyclops falls. Hero Lv ${player.level}, ${player.gold} gold.`,
    options: ['Keep Playing', 'Return to Title'],
    color: '#8fff8f',
  });
}
