// Projectiles for both sides: hero Slash Wave and monster bone/rock throws.
let nextProjectileId = 1;

export class Projectile {
  /**
   * @param {{x:number,y:number,vx:number,vy:number,owner:'player'|'monster',damage:number,
   *          spriteKey:string,lifeMs:number,w:number,h:number,pierce?:boolean,scale?:number,
   *          explodeRadius?:number,explodeFx?:string,knockback?:number}} opts
   * `explodeRadius` > 0 makes it burst on first contact (AoE, see projectile-hit-system.js).
   */
  constructor(opts) {
    this.id = nextProjectileId++;
    this.x = opts.x;
    this.y = opts.y;
    this.vx = opts.vx;
    this.vy = opts.vy;
    this.owner = opts.owner;
    this.damage = opts.damage;
    this.spriteKey = opts.spriteKey;
    this.lifeMs = opts.lifeMs;
    this.w = opts.w;
    this.h = opts.h;
    this.pierce = Boolean(opts.pierce);
    this.scale = opts.scale ?? (opts.owner === 'player' ? 0.7 : 0.4);
    this.explodeRadius = opts.explodeRadius || 0;
    this.explodeFx = opts.explodeFx || 'arcaneBurst'; // effect drawn when it bursts
    this.knockback = opts.knockback || 0;
    this.rotation = Math.atan2(opts.vy, opts.vx);
    this.hitIds = new Set();
    this.dead = false;
  }

  /** Collision box centered on the projectile. */
  get box() {
    return { x: this.x - this.w / 2, y: this.y - this.h / 2, w: this.w, h: this.h };
  }

  update(dt) {
    const seconds = dt / 1000;
    this.x += this.vx * seconds;
    this.y += this.vy * seconds;
    this.lifeMs -= dt;
    if (this.lifeMs <= 0) this.dead = true;
  }
}
