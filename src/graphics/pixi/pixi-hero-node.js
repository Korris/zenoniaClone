// Retained PixiJS node for a hero (also the Assassin's shadow clone): shadow, two dash afterimages, the body
// frame and up to MAX_GEAR held-gear slots (staff / sword / shield / daggers). Frame choice, gear resolution and
// anchors come from player-renderer.js / held-weapon-renderer.js -- this file only translates their output to sprites.
import { heroFrame } from '../player-renderer.js';
import { resolveHeldGear } from '../held-weapon-renderer.js';
import { placeSprite, placeShadow } from './pixi-actor-node.js';
import { depthKey, snapToDevice } from './depth-key.js';

const FRAME_PX = 192; // source px of a hero frame: gear anchors are expressed in these units
const AURA_PULSE_MS = 900;
const MAX_GEAR = 4;
const Z = { shadow: 0, ghost: 1, behind: 3, body: 4, front: 5 };

/** One overlay (aura + body of a gear piece) hung on a pivot at its grip anchor. */
export class GearSlot {
  constructor(PIXI) {
    this.root = new PIXI.Container();
    this.aura = new PIXI.Sprite();
    this.body = new PIXI.Sprite();
    this.root.addChild(this.aura, this.body);
    this.root.visible = false;
  }

  /** Mirrors held-weapon-renderer.js drawGear: (x, y) = frame top-left in world units, `size` = frame size in units. */
  place(layer, x, y, size, env) {
    const { anchor } = layer;
    // Enhanced gear: live GlowFilter around the plain art (env.glow on) instead of the baked outline rings.
    const live = Boolean(env.glow && layer.art && layer.band >= 1);
    const baked = live ? { body: layer.art.img, aura: null, gripX: layer.art.gripX, gripY: layer.art.gripY } : layer.baked;
    this.setGlow(live ? layer : null, env);
    const unit = size / FRAME_PX;
    const k = anchor.s * unit * (layer.drawScale || 1); // world units per overlay pixel
    this.root.visible = true;
    this.root.position.set(x + anchor.x * unit, y + anchor.y * unit);
    this.root.rotation = ((anchor.angle || 0) * Math.PI) / 180;
    const sx = anchor.sx && anchor.sx !== 1 ? Math.sign(anchor.sx) * Math.max(Math.abs(anchor.sx), layer.minSx || 0) : 1;
    this.root.scale.x = sx;
    const dx = -baked.gripX * k, dy = -baked.gripY * k;
    const draw = (sprite, source) => placeSprite(sprite, env.tex.forSize(source, source.width * k, source.height * k, env.scale), dx, dy, source.width * k, source.height * k, env.scale * 4);
    this.aura.visible = Boolean(baked.aura);
    if (baked.aura) {
      draw(this.aura, baked.aura);
      this.aura.alpha = 0.775 + 0.225 * Math.sin((env.now / AURA_PULSE_MS) * Math.PI * 2);
    }
    draw(this.body, baked.body);
  }

  /** Attach / tune / detach this slot's GlowFilter. */
  setGlow(layer, env) {
    if (!layer) { if (this.glow) { this.root.filters = []; this.glow = null; } return; }
    if (!this.glow) { this.glow = env.glow.create(); this.root.filters = [this.glow]; }
    env.glow.tune(this.glow, layer.band, layer.glowColor, env.scale, env.bloomOn, env.now);
  }
}

export class HeroNode {
  constructor(env) {
    const { PIXI, prim } = env;
    this.root = new PIXI.Container();
    this.root.sortableChildren = true;
    this.shadow = new PIXI.Sprite(prim.ellipse);
    this.shadow.anchor.set(0.5); this.shadow.tint = 0x000000; this.shadow.alpha = 0.3; this.shadow.zIndex = Z.shadow;
    this.ghosts = [0.35, 0.2].map((alpha) => { const s = new PIXI.Sprite(); s.alpha = alpha; s.zIndex = Z.ghost; return s; });
    this.body = new PIXI.Sprite();
    this.body.zIndex = Z.body;
    this.slots = Array.from({ length: MAX_GEAR }, () => new GearSlot(PIXI));
    this.root.addChild(this.shadow, ...this.ghosts, this.body, ...this.slots.map((s) => s.root));
  }

  /** @param {number} alpha overall opacity (shadow clone ghosting) */
  update(player, registry, env, alpha = 1) {
    const frame = heroFrame(registry.heroes?.[player.classId] || registry.hero, player);
    const size = registry.heroFrameUnits;
    const x = snapToDevice(player.x - size / 2, env.scale), y = snapToDevice(player.y - size, env.scale);
    const held = resolveHeldGear(registry, player, frame);
    const source = held ? held.body : frame;
    const tex = env.tex.forSize(source, size, size, env.scale);
    this.root.visible = true;
    this.root.zIndex = depthKey('player', player.y);
    this.root.alpha = alpha * (player.stealth ? 0.5 : 1); // Smoke Bomb: half-transparent hero
    placeShadow(this.shadow, player.x, player.y, 6, 2.5, env.scale);
    placeSprite(this.body, tex, x, y, size, size, env.scale);
    this.ghosts.forEach((ghost, i) => {
      ghost.visible = Boolean(player.dash);
      if (player.dash) placeSprite(ghost, tex, x - player.dash.dirX * 6 * (i + 1), y - player.dash.dirY * 6 * (i + 1), size, size, env.scale);
    });
    const layers = held ? held.layers.filter((l) => !l.anchor.hidden) : [];
    this.slots.forEach((slot, i) => {
      const layer = layers[i];
      if (!layer) { slot.root.visible = false; return; }
      slot.root.zIndex = layer.anchor.behind ? Z.behind : Z.front;
      slot.place(layer, x, y, size, env);
    });
  }

  hide() {
    this.root.visible = false;
  }
}
