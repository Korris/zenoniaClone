// Generic repeating timer for multi-hit skills (e.g. Blade Storm's 3 pulses). Decoupled from
// combat logic: the caller supplies `onPulse`, this just owns the countdown + `.dead` lifecycle
// so world-update.js can tick it the same way as every other timed list.
export class SkillPulse {
  /** @param {{remaining:number, intervalMs:number, onPulse:() => void}} opts */
  constructor(opts) {
    this.remaining = opts.remaining;
    this.intervalMs = opts.intervalMs;
    this.onPulse = opts.onPulse;
    this.timer = opts.intervalMs;
    this.dead = this.remaining <= 0;
  }

  update(dt) {
    if (this.dead) return;
    this.timer -= dt;
    if (this.timer > 0) return;
    this.timer += this.intervalMs;
    this.onPulse();
    this.remaining -= 1;
    if (this.remaining <= 0) this.dead = true;
  }
}
