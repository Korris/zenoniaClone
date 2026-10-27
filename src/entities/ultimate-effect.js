// Fixed-world timelines. All events are consumed once, including after a long frame.
export class UltimateEffect {
  constructor({ kind, caster, x, y, radius, dir, count = 8, range = 60, onEvent }) {
    Object.assign(this, { kind, caster, x, y, radius, dir, onEvent });
    this.casterX = caster.x; this.casterY = caster.y;
    this.age = 0; this.cursor = 0; this.dead = false;
    this.lockMs = kind === 'earthBreak' ? 1450 : 850;
    if (kind === 'earthBreak') {
      this.events = [{ at: 560, type: 'sweep' }, { at: 1450, type: 'slam' },
        ...[1700, 1950, 2200, 2450].map(at => ({ at, type: 'burn' }))];
      this.durationMs = 2500;
    } else {
      const angle = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 }[dir];
      this.rockets = Array.from({ length: count }, (_, i) => {
        const a = angle + (i / Math.max(1, count - 1) - 0.5) * Math.PI / 3;
        const distance = range * (0.65 + (i % 3) * 0.16);
        return { x: caster.x + Math.cos(a) * distance, y: caster.y - 6 + Math.sin(a) * distance,
          launchMs: 400 + (i % 3) * 40, impactMs: 1550 + i * 85 };
      });
      this.events = this.rockets.map((rocket, i) => ({ at: rocket.impactMs, type: 'rocket', rocket, index: i }));
      this.durationMs = this.rockets.at(-1).impactMs + 850;
    }
  }
  update(dt) {
    if (this.dead) return;
    if (this.caster.dead) { this.dead = true; return; }
    this.age += dt;
    while (this.cursor < this.events.length && this.age >= this.events[this.cursor].at) {
      const event = this.events[this.cursor++]; this.onEvent?.(event);
    }
    if (this.age >= this.durationMs) this.dead = true;
  }
}
