// Melee game-feel VFX in the PixiJS scene, driven by the same world.feel state as combat-feel-renderer.js:
//  - weapon trails  -> two textured ribbon meshes (class-coloured edge + additive white core)
//  - spark streaks  -> GPU particles (capsule sprites aligned to their velocity, plus faded echoes behind them)
//  - impact stars / shock rings -> small pooled sprites (coloured edge + additive white core)
//  - generic hit particles (world.particles) -> GPU particles
// Everything lives in the bloom layer, so the white cores bloom while the coloured edges stay saturated.
import { COMBAT_FEEL } from '../../data/combat-feel-config.js';
import { ParticleBatch } from './pixi-particle-batch.js';
import { TrailMesh } from './pixi-trail-mesh.js';
import { hexToInt } from './pixi-primitives.js';

const MAX_TRAILS = 6;
const HIT_PARTICLE_CAPACITY = 256;
const HIT_PARTICLE_SIZE = 3.5;

export class CombatFeelLayer {
  constructor(env, container) {
    const { PIXI, prim } = env;
    this.env = env;
    const { impact } = COMBAT_FEEL;
    const echoes = COMBAT_FEEL.pixi.spark.echoes;
    this.trailsEdge = Array.from({ length: MAX_TRAILS }, () => new TrailMesh(PIXI, prim.trail));
    this.trailsCore = Array.from({ length: MAX_TRAILS }, () => new TrailMesh(PIXI, prim.trail, { additive: true }));
    this.ringsEdge = Array.from({ length: impact.ringPoolSize }, () => this.sprite(prim.ring));
    this.ringsCore = Array.from({ length: impact.ringPoolSize }, () => this.sprite(prim.ring, true));
    this.starsEdge = Array.from({ length: impact.starPoolSize }, () => this.sprite(prim.star));
    this.starsCore = Array.from({ length: impact.starPoolSize }, () => this.sprite(prim.star, true));
    const cap = impact.poolSize * (1 + echoes);
    this.sparksEdge = new ParticleBatch(PIXI, prim.streak, cap);
    this.sparksCore = new ParticleBatch(PIXI, prim.streak, impact.poolSize, 'add');
    this.hitTexture = null;
    this.knownRings = new Set(); // rings already seen alive: a new one = a finisher / crit impact -> shockwave
    this.hitParticles = null;
    container.addChild(...this.trailsEdge.map((t) => t.mesh), ...this.ringsEdge, this.sparksEdge.container, ...this.starsEdge,
      ...this.trailsCore.map((t) => t.mesh), ...this.ringsCore, this.sparksCore.container, ...this.starsCore);
    this.container = container;
  }

  /** How many feel / particle items are alive this frame (0 => nothing to bloom). */
  activeCount(world) {
    const f = world.feel;
    return world.particles.length + (f ? f.trails.length + f.streaks.activeCount + f.rings.activeCount + f.stars.activeCount : 0);
  }

  sprite(tex, additive = false) {
    const s = new this.env.PIXI.Sprite(tex);
    s.anchor.set(0.5);
    s.visible = false;
    if (additive) s.blendMode = 'add';
    return s;
  }

  update(world, registry) {
    const { feel } = world;
    const { trail, impact } = COMBAT_FEEL;
    const px = COMBAT_FEEL.pixi;
    if (feel) {
      this.updateTrails(feel, trail, px);
      this.updateSparks(feel, impact, px);
      this.updatePool(this.starsEdge, this.starsCore, feel.stars.items, impact, (s, core, star) => {
        const t = star.ageMs / star.lifeMs;
        const r = star.size * (t < 0.3 ? 0.5 + (t / 0.3) * 0.7 : 1.2 * (1 - (t - 0.3) / 0.7));
        if (r <= 0.2) return false;
        const d = ((core ? r * 0.55 : r) * 2) / 62;
        s.position.set(star.x, star.y); s.rotation = star.rotation; s.scale.set(d);
        s.tint = hexToInt(core ? impact.colors.core : star.crit ? impact.colors.crit : impact.colors.edge);
        return true;
      });
      for (const ring of feel.rings.items) {
        if (!ring.active) this.knownRings.delete(ring);
        else if (!this.knownRings.has(ring)) { this.knownRings.add(ring); this.env.shock?.(ring.x, ring.y); }
      }
      this.updatePool(this.ringsEdge, this.ringsCore, feel.rings.items, impact, (s, core, ring) => {
        const t = ring.ageMs / ring.lifeMs;
        const radius = 2 + ring.maxRadius * (1 - (1 - t) * (1 - t));
        s.position.set(ring.x, ring.y); s.scale.set(((core ? 0.96 : 1) * radius * 2) / 128);
        s.alpha = 1 - t * t;
        s.tint = hexToInt(core ? impact.colors.core : ring.crit ? impact.colors.crit : impact.colors.edge);
        return true;
      });
    }
    this.updateHitParticles(world.particles, registry);
  }

  updateTrails(feel, cfg, px) {
    this.trailsEdge.forEach((edge, i) => {
      const trail = feel.trails[i];
      if (!trail) { edge.mesh.visible = this.trailsCore[i].mesh.visible = false; return; }
      const colors = trail.colors;
      edge.update(trail, 1, hexToInt(colors.edge), cfg.edgeAlpha);
      this.trailsCore[i].update(trail, 0.42, hexToInt(colors.core), px.trailGlow);
    });
  }

  updateSparks(feel, impact, px) {
    const { echoes, echoSpacing, echoAlpha } = px.spark;
    const { edge, core, crit } = impact.colors;
    const [edgeInt, coreInt] = [hexToInt(edge), hexToInt(core)];
    this.sparksEdge.begin(); this.sparksCore.begin();
    for (const s of feel.streaks.items) {
      if (!s.active) continue;
      const life = 1 - s.ageMs / s.lifeMs;
      const speed = Math.hypot(s.vx, s.vy) || 1;
      const len = speed * impact.sparkLength * (0.35 + 0.65 * life) + 1.2;
      const rot = Math.atan2(s.vy, s.vx), dx = s.vx / speed, dy = s.vy / speed;
      const tint = s.crit ? hexToInt(crit) : edgeInt;
      this.sparksEdge.add(s.x, s.y, rot, len / 32, 1.2 / 8, tint, 1, 1);
      this.sparksCore.add(s.x, s.y, rot, len / 32, 0.5 / 8, coreInt, 1, 1);
      for (let e = 1; e <= echoes; e++) { // faded copies trailing behind: denser spray for free on the GPU
        const back = len * echoSpacing * e;
        this.sparksEdge.add(s.x - dx * back, s.y - dy * back, rot, (len * 0.7) / 32, 0.9 / 8, tint, echoAlpha / e, 1);
      }
    }
    this.sparksEdge.end(); this.sparksCore.end();
  }

  /** Shows one edge+core sprite pair per active pooled item; `fill` returns false to hide an item. */
  updatePool(edgeSprites, coreSprites, items, impact, fill) {
    let n = 0;
    for (const item of items) {
      if (!item.active || n >= edgeSprites.length) continue;
      const edge = edgeSprites[n], core = coreSprites[n];
      edge.alpha = core.alpha = 1;
      if (fill(edge, false, item) && fill(core, true, item)) { edge.visible = core.visible = true; n++; }
    }
    for (let i = n; i < edgeSprites.length; i++) edgeSprites[i].visible = coreSprites[i].visible = false;
  }

  updateHitParticles(particles, registry) {
    const { PIXI, prim, tex } = this.env;
    const source = registry?.effects.hitParticle;
    if (!this.hitParticles) {
      this.hitTexture = source ? tex.of(source) : prim.star;
      this.hitParticles = new ParticleBatch(PIXI, this.hitTexture, HIT_PARTICLE_CAPACITY);
      this.container.addChild(this.hitParticles.container);
    }
    const scale = (source ? HIT_PARTICLE_SIZE : 2.2) / this.hitTexture.width;
    this.hitParticles.begin();
    for (const p of particles) this.hitParticles.add(p.x, p.y, 0, scale, scale, source ? 0xffffff : 0xffe680, Math.min(1, Math.max(0, p.lifeMs / 120)));
    this.hitParticles.end();
  }
}
