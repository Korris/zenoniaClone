// Assassin's Shadow Clone: a purple afterimage that trails the hero for `durationMs`. While it lives, every
// basic hit the hero lands is echoed around the clone (see assassin-combo-riders.js). Lives in world.effects
// (kind 'shadowClone'); drawn by shadow-clone-renderer.js as a translucent copy of the hero.
const FOLLOW_MS = 140; // easing time constant toward the hero's side
const FADE_MS = 500;

export class ShadowClone {
  /** @param {{owner:object, durationMs:number, mult:number, radius:number}} opts owner = the Player */
  constructor({ owner, durationMs, mult, radius }) {
    this.kind = 'shadowClone';
    Object.assign(this, { owner, durationMs, mult, radius });
    this.x = owner.x - 14;
    this.y = owner.y + 2;
    this.age = 0;
    this.dead = false;
  }

  update(dt) {
    if (this.dead) return;
    this.age += dt;
    const k = Math.min(1, dt / FOLLOW_MS);
    this.x += (this.owner.x - 14 - this.x) * k;
    this.y += (this.owner.y + 2 - this.y) * k;
    if (this.age >= this.durationMs || this.owner.dead) this.dead = true;
  }

  /** 0..1 opacity factor: fades out over the last FADE_MS. */
  get intensity() {
    return Math.max(0, Math.min(1, (this.durationMs - this.age) / FADE_MS));
  }
}
