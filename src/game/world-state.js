// Container for everything alive in the current map. Systems read/write through this object.
import { FloatingText } from '../entities/floating-damage-number.js';
import { VisualEffect, SparkParticle } from '../entities/visual-effect.js';

export class WorldState {
  constructor(player, camera) {
    this.player = player;
    this.camera = camera;
    this.mapId = null;
    this.mapDef = null;
    this.tilemap = null;
    this.spawner = null;
    this.props = []; // static decorations {type, x, y} drawn y-sorted with actors
    this.npcs = [];
    this.monsters = [];
    this.projectiles = [];
    this.drops = [];
    this.effects = [];
    this.particles = [];
    this.texts = [];
    this.skillPulses = []; // repeating multi-hit skills (e.g. Blade Storm), see entities/skill-pulse.js
    this.bossDefeated = false;
    this.swingCounter = 0; // unique id per hero swing for per-target dedupe
    this.elapsedMs = 0; // for tile animation
    this.hitStopMs = 0; // brief freeze on heavy hits
    this.events = []; // per-frame gameplay events (levelUp, playerDied, bossDefeated)
  }

  /** Clear per-map entities (keeps player, camera, progression flags). */
  clearMapEntities() {
    this.props = [];
    this.npcs = [];
    this.monsters = [];
    this.projectiles = [];
    this.drops = [];
    this.effects = [];
    this.particles = [];
    this.texts = [];
    this.skillPulses = [];
  }

  nextSwingId() {
    this.swingCounter += 1;
    return this.swingCounter;
  }

  addText(x, y, text, color, opts) {
    this.texts.push(new FloatingText(x, y, text, color, opts));
  }

  addEffect(opts) {
    const effect = new VisualEffect(opts);
    this.effects.push(effect);
    return effect;
  }

  addSparks(x, y, count = 4) {
    for (let i = 0; i < count; i++) this.particles.push(new SparkParticle(x, y));
  }

  /** Drop dead entities from every list. */
  removeDead() {
    this.monsters = this.monsters.filter((m) => !m.dead);
    this.projectiles = this.projectiles.filter((p) => !p.dead);
    this.drops = this.drops.filter((d) => !d.dead);
    this.effects = this.effects.filter((e) => !e.dead);
    this.particles = this.particles.filter((p) => !p.dead);
    this.texts = this.texts.filter((t) => !t.dead);
    this.skillPulses = this.skillPulses.filter((p) => !p.dead);
  }

  get livingBoss() {
    return this.monsters.find((m) => m.isBoss && !m.dead) || null;
  }
}
