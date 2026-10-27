// Floating combat text (damage numbers, "LEVEL UP!", pickups). Drifts upward and fades.
export class FloatingText {
  constructor(x, y, text, color = '#ffffff', opts = {}) {
    this.x = x;
    this.y = y;
    this.text = String(text);
    this.color = color;
    this.lifeMs = opts.lifeMs ?? 700;
    this.maxLifeMs = this.lifeMs;
    this.vy = opts.vy ?? -28; // px/s upward
    this.big = Boolean(opts.big);
    this.damageStyle = opts.damageStyle ?? null;
    this.critical = Boolean(opts.critical);
    this.dead = false;
  }

  get alpha() {
    return Math.max(0, Math.min(1, this.lifeMs / (this.maxLifeMs * 0.5)));
  }

  update(dt) {
    this.y += (this.vy * dt) / 1000;
    this.lifeMs -= dt;
    if (this.lifeMs <= 0) this.dead = true;
  }
}
