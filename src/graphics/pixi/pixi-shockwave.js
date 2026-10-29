// Short screen shockwave (pixi-filters ShockwaveFilter) on combo finisher / crit impacts. The filter is attached to the
// stage only while a wave is alive (~0.4 s). It is localized by the filter's `radius` (pixels outside it are untouched);
// a clipped filterArea is NOT used because the area outside it would not be drawn at all. HUD lives on the separate 2D
// canvas and is never distorted.
import { COMBAT_FEEL } from '../../data/combat-feel-config.js';

export class Shockwave {
  /** @param {object|null} filters pixi-filters module (null => feature off) */
  constructor(PIXI, filters, stage, getSize) {
    this.getSize = getSize;
    this.stage = stage;
    this.area = new PIXI.Rectangle(0, 0, 1, 1);
    this.startMs = 0;
    this.active = false;
    this.filter = filters ? new filters.ShockwaveFilter({ center: { x: 0, y: 0 }, time: 0, ...this.params() }) : null;
  }

  params() {
    const { speed, amplitude, wavelength, brightness } = COMBAT_FEEL.pixi.shockwave;
    return { speed, amplitude, wavelength, brightness, radius: -1 };
  }

  /** Start a wave at screen pixel (x, y); `scale` = device px per world unit (sizes are authored in world units). */
  trigger(x, y, scale, nowMs) {
    const cfg = COMBAT_FEEL.pixi.shockwave;
    if (!this.filter || !cfg.enabled || !COMBAT_FEEL.pixi.fxEnabled) return;
    const { width, height } = this.getSize();
    this.area.width = width; this.area.height = height;
    this.center = { x, y };
    this.scale = scale;
    this.startMs = nowMs;
    this.active = true;
    this.stage.filterArea = this.area;
    this.stage.filters = [this.filter];
  }

  update(nowMs) {
    if (!this.active) return;
    const cfg = COMBAT_FEEL.pixi.shockwave;
    const age = nowMs - this.startMs;
    if (age >= cfg.durationMs || !COMBAT_FEEL.pixi.fxEnabled) { this.stop(); return; }
    const f = this.filter;
    f.time = age / 1000;
    f.center = this.center;
    f.speed = cfg.speed * this.scale / 4;
    f.amplitude = cfg.amplitude * this.scale / 4 * (1 - age / cfg.durationMs);
    f.wavelength = cfg.wavelength * this.scale / 4;
    f.radius = cfg.radius * this.scale;
  }

  stop() {
    this.active = false;
    this.stage.filters = [];
    this.stage.filterArea = null;
  }
}
