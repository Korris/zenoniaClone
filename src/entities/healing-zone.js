// Ground heal-over-time zone (Cleric Healing Circle). Calls `onTick(zone)` every `tickMs` for
// `durationMs`, then dies; the caller decides who is healed. Lives in world.effects (kind 'healingZone').
export class HealingZone {
  /** @param {{x:number,y:number,radius:number,durationMs:number,tickMs:number,onTick:(z:HealingZone)=>void}} opts */
  constructor({ x, y, radius, durationMs, tickMs, onTick }) {
    this.kind = 'healingZone';
    Object.assign(this, { x, y, radius, durationMs, tickMs, onTick });
    this.age = 0;
    this.nextTickMs = tickMs;
    this.dead = false;
  }

  update(dt) {
    if (this.dead) return;
    this.age += dt;
    while (this.nextTickMs <= this.durationMs && this.age >= this.nextTickMs) {
      this.onTick(this);
      this.nextTickMs += this.tickMs;
    }
    if (this.age >= this.durationMs) this.dead = true;
  }

  /** 0..1 fade-in/out factor for drawing. */
  get intensity() {
    return Math.max(0, Math.min(1, this.age / 250, (this.durationMs - this.age) / 400));
  }
}
