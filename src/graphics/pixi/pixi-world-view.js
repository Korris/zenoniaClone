// PixiJS (WebGL) world renderer: a second canvas stacked BELOW the game's 2D canvas. It draws the whole world from
// the same game state the Canvas2D renderer reads (camera, interpolation, hit-stop and shake all come from the
// controller); the 2D canvas above stays transparent over it and keeps drawing HUD, menus and a few legacy overlays.
//
//   stage
//    |- scene  (scaled to the camera): ground -> drops -> y-sorted actors
//    '- fxRoot (bloom filter, screen-sized filterArea) -> fx (scaled): effect sprites, trails, sparks, rings, stars
import { VIEW_WIDTH } from '../../core/constants.js';
import { COMBAT_FEEL } from '../../data/combat-feel-config.js';
import { loadPixi } from './pixi-loader.js';
import { TextureCache } from './pixi-textures.js';
import { createPrimitives } from './pixi-primitives.js';
import { GroundLayer } from './pixi-ground-layer.js';
import { DropsLayer } from './pixi-drops-layer.js';
import { ActorLayer } from './pixi-actor-layer.js';
import { EffectsLayer } from './pixi-effects-layer.js';
import { CombatFeelLayer } from './pixi-combat-feel-layer.js';
import { Shockwave } from './pixi-shockwave.js';
import { createGearGlow, tuneGearGlow } from './pixi-gear-glow.js';

export class PixiWorldView {
  /** Creates the WebGL canvas next to `canvas2d` and boots PixiJS. Rejects if WebGL is unavailable. */
  static async create(canvas2d) {
    const { PIXI, filters } = await loadPixi();
    const canvas = document.createElement('canvas');
    canvas.id = 'pixi-canvas';
    canvas2d.parentElement.insertBefore(canvas, canvas2d);
    const app = new PIXI.Application();
    await app.init({ canvas, width: canvas2d.width, height: canvas2d.height, resolution: 1, autoDensity: false, antialias: false,
      background: 0x000000, preference: 'webgl', autoStart: false, powerPreference: 'high-performance' });
    app.ticker.stop();
    return new PixiWorldView(PIXI, filters, app, canvas, canvas2d);
  }

  constructor(PIXI, filters, app, canvas, canvas2d) {
    this.PIXI = PIXI;
    this.app = app;
    this.canvas = canvas;
    this.canvas2d = canvas2d;
    this.env = { PIXI, tex: new TextureCache(PIXI), prim: createPrimitives(PIXI), scale: 4, now: 0 };
    const { stage } = app;
    this.filters = filters;
    this.glowApi = filters ? { create: () => createGearGlow(filters), tune: tuneGearGlow } : null;
    this.shockwave = new Shockwave(PIXI, filters, stage, () => this.canvas2d);
    this.env.shock = (x, y) => this.shockwave.trigger((x - this.camera.renderX) * this.env.scale, (y - this.camera.renderY) * this.env.scale, this.env.scale, this.env.now);
    this.scene = new PIXI.Container();
    this.fxRoot = new PIXI.Container();
    this.fx = new PIXI.Container();
    this.fxRoot.addChild(this.fx);
    const layer = (parent) => parent.addChild(new PIXI.Container());
    this.ground = new GroundLayer(this.env, layer(this.scene));
    this.drops = new DropsLayer(this.env, layer(this.scene));
    this.actors = new ActorLayer(this.env, layer(this.scene));
    this.effects = new EffectsLayer(this.env, layer(this.fx));
    this.feel = new CombatFeelLayer(this.env, layer(this.fx));
    stage.addChild(this.scene, this.fxRoot);
    this.bloom = filters ? new filters.AdvancedBloomFilter(this.bloomOptions()) : null;
    if (this.bloom) this.bloom.resolution = COMBAT_FEEL.pixi.bloom.resolution;
    this.classId = null;
    this.bloomOn = null;
    this.fxRoot.filterArea = new PIXI.Rectangle(0, 0, canvas2d.width, canvas2d.height);
    this.syncSize();
    this.applyToggles();
  }

  bloomOptions() {
    const { threshold, bloomScale, brightness, blur, quality } = COMBAT_FEEL.pixi.bloom;
    return { threshold, bloomScale, brightness, blur, quality };
  }

  /** Keep the WebGL canvas exactly on top of the 2D canvas (same buffer size, same CSS box). */
  syncSize() {
    const { canvas, canvas2d, app } = this;
    if (canvas.width !== canvas2d.width || canvas.height !== canvas2d.height) {
      app.renderer.resize(canvas2d.width, canvas2d.height);
      this.fxRoot.filterArea = new this.PIXI.Rectangle(0, 0, canvas2d.width, canvas2d.height);
    }
    if (canvas.style.width !== canvas2d.style.width) canvas.style.width = canvas2d.style.width;
    if (canvas.style.height !== canvas2d.style.height) canvas.style.height = canvas2d.style.height;
  }

  /** `fxBusy`: the bloom pass (a full-screen blur, the priciest thing here) only runs while something is in the FX layer. */
  applyToggles(fxBusy = true) {
    const { bloom, fxEnabled } = COMBAT_FEEL.pixi;
    this.fxRoot.visible = fxEnabled;
    const want = this.bloom && bloom.enabled && fxBusy;
    if (want !== this.bloomOn) { this.bloomOn = want; this.fxRoot.filters = want ? [this.bloom] : []; }
  }

  toggleBloom() {
    COMBAT_FEEL.pixi.bloom.enabled = !COMBAT_FEEL.pixi.bloom.enabled;
    this.applyToggles();
  }

  toggleFx() {
    COMBAT_FEEL.pixi.fxEnabled = !COMBAT_FEEL.pixi.fxEnabled;
    this.applyToggles();
  }

  get status() {
    const { bloom, fxEnabled } = COMBAT_FEEL.pixi;
    return `PIXI  bloom ${this.bloom ? (bloom.enabled ? 'ON' : 'off') : 'n/a'}  fx ${fxEnabled ? 'ON' : 'off'}`;
  }

  /** New class art / map: drop every GPU texture so memory does not accumulate; they re-upload lazily. */
  releaseTextures() {
    this.env.tex.clear();
    this.ground.mapKey = null;
    this.drops.clear();
  }

  /** Draw one world frame (positions are already interpolated by the caller). */
  render(world, registry, view) {
    const { env, canvas, scene, fx } = this;
    this.syncSize();
    if (world.player.classId !== this.classId) { if (this.classId) this.releaseTextures(); this.classId = world.player.classId; }
    env.scale = canvas.width / VIEW_WIDTH; // device pixels per world unit
    env.now = performance.now();
    const { camera } = world;
    this.camera = camera;
    const { pixi } = COMBAT_FEEL;
    env.bloomOn = Boolean(this.bloom && pixi.bloom.enabled);
    env.glow = pixi.fxEnabled && pixi.gearGlow.enabled ? this.glowApi : null; // F8 off => baked rings, like Canvas2D
    for (const root of [scene, fx]) {
      root.scale.set(env.scale);
      root.position.set(-Math.round(camera.renderX * env.scale), -Math.round(camera.renderY * env.scale));
    }
    this.ground.update(world, registry);
    this.drops.update(world, registry);
    this.actors.update(world, registry, view);
    this.effects.update(world, registry);
    this.feel.update(world, registry);
    this.applyToggles(this.effects.set.size + this.feel.activeCount(world) > 0);
    this.shockwave.update(env.now);
    this.app.renderer.render(this.app.stage);
  }

  destroy() {
    this.app.destroy(true, { children: true });
  }
}
