// One bounded timeline owns the rain and its four damage waves; it never follows the caster.
export const ICE_STORM_CAST_MS = 650;
export const ICE_STORM_FALL_MS = 560;
export const ICE_STORM_WAVE_MS = 400;
export const ICE_STORM_TAIL_MS = 850;
export class IceStorm {
  constructor({ x, y, casterX, casterY, radius, onWave }) {
    this.kind = 'iceStorm';
    Object.assign(this, { x, y, casterX, casterY, radius, onWave });
    this.age = 0; this.wave = 0; this.dead = false;
    this.durationMs = ICE_STORM_CAST_MS + 3 * ICE_STORM_WAVE_MS + ICE_STORM_TAIL_MS;
    this.meteors = Array.from({ length: 12 }, (_, i) => {
      const angle = i * 2.3999632297, distance = radius * Math.sqrt((i + 0.5) / 12) * 0.88;
      return { x: x + Math.cos(angle) * distance, y: y + Math.sin(angle) * distance * 0.72,
        impactMs: ICE_STORM_CAST_MS + Math.floor(i / 3) * ICE_STORM_WAVE_MS, size: 25 + (i % 3) * 4 };
    });
  }
  update(dt) {
    if (this.dead) return;
    this.age += dt;
    while (this.wave < 4 && this.age >= ICE_STORM_CAST_MS + this.wave * ICE_STORM_WAVE_MS) {
      this.onWave?.(this.wave);
      this.wave += 1;
    }
    if (this.age >= this.durationMs) this.dead = true;
  }
}
