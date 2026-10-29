// Frame-animated FX sprites in the PixiJS scene: projectiles (bolts, bullets, rockets) and VisualEffect strips
// (slashes, bursts, whirlwinds). Same rules as effects-renderer.js drawRotated -- centred, rotated, optional flipY,
// fade alpha, tint wash, and an extra additive pass for bright cores -- but each is a retained pair of sprites
// (normal + additive) living inside the bloom layer, so slashes and bursts glow.
import { pickFrame } from '../effects-renderer.js';
import { tintedSprite } from '../tinted-sprite-cache.js';
import { RetainedSet, NodePool } from './retained-set.js';
import { snapToDevice } from './depth-key.js';

// Drawn by the 2D overlay or elsewhere (zones, storms, ultimates) or owned by the actor layer (clone).
const NOT_SPRITE_EFFECTS = new Set(['earthBreak', 'rocketBarrage', 'iceStorm', 'healingZone', 'venomZone', 'shadowClone']);

class FxNode {
  constructor(PIXI) {
    this.base = new PIXI.Sprite();
    this.glow = new PIXI.Sprite();
    for (const s of [this.base, this.glow]) s.anchor.set(0.5);
    this.glow.blendMode = 'add';
    this.root = new PIXI.Container();
    this.root.addChild(this.base, this.glow);
  }

  place(source, x, y, rotation, scale, alpha, flipY, additive, env) {
    const ppu = source.pixelsPerUnit || 1;
    const w = (source.width / ppu) * scale, h = (source.height / ppu) * scale;
    const tex = env.tex.forSize(source, w, h, env.scale);
    this.root.visible = true;
    this.root.position.set(snapToDevice(x, env.scale), snapToDevice(y, env.scale));
    this.root.rotation = rotation;
    for (const s of [this.base, this.glow]) {
      if (s.texture !== tex) s.texture = tex;
      s.scale.set(w / tex.width, (flipY ? -h : h) / tex.height);
    }
    this.base.alpha = alpha;
    this.glow.visible = additive > 0;
    this.glow.alpha = alpha * additive;
  }
}

export class EffectsLayer {
  constructor(env, container) {
    this.env = env;
    const pool = new NodePool(() => { const n = new FxNode(env.PIXI); container.addChild(n.root); return n; }, (n) => { n.root.visible = false; });
    this.set = new RetainedSet(() => pool.take(), (n) => pool.put(n));
  }

  update(world, registry) {
    const { env } = this;
    for (const p of world.projectiles) {
      const entry = registry.effects[p.spriteKey];
      if (!entry) continue;
      const sprite = pickFrame(entry, 0.4);
      this.set.get(p).place(p.tint ? tintedSprite(sprite, p.tint) : sprite, p.x, p.y - 6, p.rotation, p.scale, 1, false, 0, env);
    }
    for (const e of world.effects) {
      if (NOT_SPRITE_EFFECTS.has(e.kind)) continue;
      const entry = registry.effects[e.spriteKey];
      if (!entry) continue;
      const frame = pickFrame(entry, e.progress);
      this.set.get(e).place(e.tint ? tintedSprite(frame, e.tint) : frame, e.x, e.y, e.rotation, e.scale, e.alpha, e.flipY, e.additive, env);
    }
    this.set.endFrame();
  }
}
