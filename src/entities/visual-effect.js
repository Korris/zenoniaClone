// Short-lived visual effects: slash arcs, whirlwind ring, spark particles.
export class VisualEffect {
  /**
   * @param {{x:number,y:number,spriteKey:string,rotation?:number,lifeMs?:number,
   *          scale?:number,follow?:object,offsetX?:number,offsetY?:number,spin?:number,flipY?:boolean,tint?:string}} opts
   */
  constructor(opts) {
    this.x = opts.x;
    this.y = opts.y;
    this.spriteKey = opts.spriteKey;
    this.rotation = opts.rotation ?? 0;
    this.flipY = Boolean(opts.flipY);
    this.lifeMs = opts.lifeMs ?? 120;
    this.maxLifeMs = this.lifeMs;
    this.scale = opts.scale ?? 1;
    this.follow = opts.follow || null; // entity to track (e.g. whirlwind around hero)
    this.offsetX = opts.offsetX ?? 0;
    this.offsetY = opts.offsetY ?? 0;
    this.spin = opts.spin ?? 0; // radians per second
    this.tint = opts.tint || null; // CSS colour washed over the sprite (e.g. purple Assassin slashes)
    this.dead = false;
  }

  get alpha() {
    return Math.max(0.2, this.lifeMs / this.maxLifeMs);
  }

  /** 0..1 fraction of the lifetime elapsed, used to pick a strip frame. */
  get progress() {
    return Math.min(1, 1 - this.lifeMs / this.maxLifeMs);
  }

  update(dt) {
    if (this.follow) {
      this.x = this.follow.x + this.offsetX;
      this.y = this.follow.y + this.offsetY;
    }
    this.rotation += (this.spin * dt) / 1000;
    this.lifeMs -= dt;
    if (this.lifeMs <= 0) this.dead = true;
  }
}

/** Tiny pixel sparks that fly out on hit. */
export class SparkParticle {
  constructor(x, y, rand = Math.random) {
    this.x = x;
    this.y = y;
    const angle = rand() * Math.PI * 2;
    const speed = 40 + rand() * 60;
    this.vx = Math.cos(angle) * speed;
    this.vy = Math.sin(angle) * speed;
    this.lifeMs = 200 + rand() * 150;
    this.spriteKey = 'spark';
    this.dead = false;
  }

  update(dt) {
    const s = dt / 1000;
    this.x += this.vx * s;
    this.y += this.vy * s;
    this.vx *= 0.9;
    this.vy *= 0.9;
    this.lifeMs -= dt;
    if (this.lifeMs <= 0) this.dead = true;
  }
}
