// Pure "game feel" state for melee combat: weapon trails and pooled impact VFX (sparks, rings, stars)
// plus the hit-stop helper. No DOM / canvas -> unit-testable. Rendering lives in
// graphics/combat-feel-renderer.js; tuning lives in data/combat-feel-config.js.
import { COMBAT_FEEL } from '../data/combat-feel-config.js';

/** Fixed-size pool: acquire() returns a free slot or null when full (spawns are dropped, never allocated). */
export class Pool {
  constructor(size, make) {
    this.items = Array.from({ length: size }, () => ({ ...make(), active: false }));
  }

  acquire() {
    for (const item of this.items) if (!item.active) { item.active = true; return item; }
    return null;
  }

  get activeCount() {
    let n = 0;
    for (const item of this.items) if (item.active) n++;
    return n;
  }

  clear() {
    for (const item of this.items) item.active = false;
  }
}

/** Remaining freeze after requesting `addMs` more: stacks but never exceeds `maxMs`. */
export function stackHitStop(currentMs, addMs, maxMs = COMBAT_FEEL.hitStop.maxMs) {
  if (!(addMs > 0)) return Math.max(0, currentMs);
  return Math.min(maxMs, Math.max(0, currentMs) + addMs);
}

/** Hit-stop duration for one landed melee hit (misses never call this). */
export function hitStopFor({ finisher = false, crit = false }, cfg = COMBAT_FEEL.hitStop) {
  return (finisher ? cfg.finisherMs : cfg.normalMs) + (crit ? cfg.critBonusMs : 0);
}

/** Swept ribbon that follows the weapon tip; samples fade `lifeMs` after the tip passes them. */
export class WeaponTrail {
  /**
   * @param {{follow:{x:number,y:number}, offsetX?:number, offsetY?:number, startAngle:number, endAngle:number,
   *          radius:number, width:number, sweepMs:number, lifeMs?:number, colors:{edge:string,core:string}}} o
   */
  constructor(o) {
    this.follow = o.follow;
    this.offsetX = o.offsetX ?? 0;
    this.offsetY = o.offsetY ?? 0;
    this.startAngle = o.startAngle;
    this.endAngle = o.endAngle;
    this.radius = o.radius;
    this.width = o.width;
    this.sweepMs = Math.max(1, o.sweepMs);
    this.lifeMs = o.lifeMs ?? COMBAT_FEEL.trail.lifeMs;
    this.colors = o.colors;
    this.ageMs = this.sweepMs * (o.lead ?? 0);
    this.dead = false;
  }

  /** 0..1 how far the tip has travelled along the arc. */
  get head() {
    return Math.min(1, this.ageMs / this.sweepMs);
  }

  /** Earliest arc fraction still visible. */
  get tail() {
    return Math.max(0, (this.ageMs - this.lifeMs) / this.sweepMs);
  }

  update(dt) {
    this.ageMs += dt;
    if (this.ageMs >= this.sweepMs + this.lifeMs) this.dead = true;
  }
}

const rand = (r, lo, hi) => lo + r() * (hi - lo);

export class CombatFeel {
  constructor(cfg = COMBAT_FEEL) {
    this.cfg = cfg;
    const i = cfg.impact;
    this.trails = [];
    this.streaks = new Pool(i.poolSize, () => ({ x: 0, y: 0, vx: 0, vy: 0, ageMs: 0, lifeMs: 1, crit: false }));
    this.rings = new Pool(i.ringPoolSize, () => ({ x: 0, y: 0, ageMs: 0, lifeMs: 1, maxRadius: 1, crit: false }));
    this.stars = new Pool(i.starPoolSize, () => ({ x: 0, y: 0, ageMs: 0, lifeMs: 1, size: 1, rotation: 0, crit: false }));
  }

  /** Start a sword/dagger ribbon. */
  addTrail(opts) {
    const trail = new WeaponTrail(opts);
    this.trails.push(trail);
    return trail;
  }

  /**
   * Spawn the impact burst at a hit point.
   * @param {{x:number,y:number,angle?:number,finisher?:boolean,crit?:boolean,ring?:boolean}} o angle = direction the hit travels
   */
  impact(o, random = Math.random) {
    const i = this.cfg.impact;
    const kind = o.crit ? 'crit' : o.finisher ? 'finisher' : 'normal';
    const angle = o.angle ?? 0;
    for (let n = 0; n < i.sparks[kind]; n++) {
      const s = this.streaks.acquire();
      if (!s) break; // pool exhausted: drop the spark rather than allocate
      const a = angle + (random() - 0.5) * Math.PI * 1.3;
      const speed = rand(random, i.sparkSpeed[0], i.sparkSpeed[1]);
      s.x = o.x; s.y = o.y; s.interpPrevX = o.x; s.interpPrevY = o.y;
      s.vx = Math.cos(a) * speed; s.vy = Math.sin(a) * speed;
      s.ageMs = 0; s.lifeMs = rand(random, i.sparkLifeMs[0], i.sparkLifeMs[1]); s.crit = kind === 'crit';
    }
    const star = this.stars.acquire();
    if (star) {
      star.x = o.x; star.y = o.y; star.ageMs = 0; star.lifeMs = i.starLifeMs;
      star.size = i.starSize[kind]; star.rotation = random() * Math.PI; star.crit = kind === 'crit';
    }
    if (kind !== 'normal' && o.ring !== false && !this.cfg.reduceEffects) { // ring:false = another target of the same swing already has one
      const ring = this.rings.acquire();
      if (ring) {
        ring.x = o.x; ring.y = o.y; ring.ageMs = 0; ring.lifeMs = i.ringLifeMs;
        ring.maxRadius = i.ringMaxRadius[kind]; ring.crit = kind === 'crit';
      }
    }
  }

  /** Impact VFX keep animating through hit-stop so the freeze frame still feels alive. */
  updateImpacts(dt) {
    const drag = Math.max(0, 1 - (5 * dt) / 1000);
    for (const s of this.streaks.items) {
      if (!s.active) continue;
      s.ageMs += dt;
      if (s.ageMs >= s.lifeMs) { s.active = false; continue; }
      s.x += (s.vx * dt) / 1000; s.y += (s.vy * dt) / 1000;
      s.vx *= drag; s.vy *= drag;
    }
    for (const pool of [this.rings, this.stars]) {
      for (const p of pool.items) {
        if (!p.active) continue;
        p.ageMs += dt;
        if (p.ageMs >= p.lifeMs) p.active = false;
      }
    }
  }

  /** Trails freeze with the world during hit-stop (the blade is stopped mid-swing). */
  updateTrails(dt) {
    for (const t of this.trails) t.update(dt);
    if (this.trails.some((t) => t.dead)) this.trails = this.trails.filter((t) => !t.dead);
  }

  clear() {
    this.trails = [];
    this.streaks.clear(); this.rings.clear(); this.stars.clear();
  }
}
