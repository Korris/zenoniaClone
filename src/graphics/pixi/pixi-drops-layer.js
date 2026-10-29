// Ground loot in the PixiJS scene (drawn under the actors like the Canvas2D renderer does). Coins, potions and
// upgrade stones are code-drawn in 2D; each kind is painted ONCE into a small canvas here and used as a texture.
// Equipment drops reuse the inventory art through effects-renderer.js equipmentDropArt().
import { drawGoldCoin, equipmentDropArt, DROP_SIZE } from '../effects-renderer.js';
import { drawPotion } from '../potion-renderer.js';
import { ActorNode, placeSprite, placeShadow } from './pixi-actor-node.js';
import { RetainedSet, NodePool } from './retained-set.js';

const BAKE_PX = 48; // design units (drawGoldCoin / drawPotion are authored on a 48 grid)

/** Canvas of a code-drawn drop, authored at `size` = BAKE_PX centred in the canvas; tagged so it samples smoothly. */
function paintDrop(kind) {
  const canvas = document.createElement('canvas');
  canvas.width = canvas.height = BAKE_PX;
  const ctx = canvas.getContext('2d');
  if (kind === 'coin') drawGoldCoin(ctx, BAKE_PX / 2, BAKE_PX / 2, BAKE_PX);
  else drawPotion(ctx, kind, BAKE_PX / 2, BAKE_PX / 2, BAKE_PX);
  canvas.pixelsPerUnit = BAKE_PX / DROP_SIZE;
  return canvas;
}

const PROCEDURAL = new Set(['coin', 'hpPotion', 'mpPotion', 'upgradeStone']);

export class DropsLayer {
  constructor(env, container) {
    this.env = env;
    this.painted = new Map();
    const pool = new NodePool(() => { const n = new ActorNode(env, 'drop'); container.addChild(n.root); return n; }, (n) => n.hide());
    this.set = new RetainedSet(() => { const n = pool.take(); n.root.visible = true; return n; }, (n) => pool.put(n));
  }

  /** The art + world-unit footprint for a drop (x/y relative to the drop's ground point), or null. */
  art(drop, registry, classId) {
    if (PROCEDURAL.has(drop.kind)) {
      if (!this.painted.has(drop.kind)) this.painted.set(drop.kind, paintDrop(drop.kind));
      return { source: this.painted.get(drop.kind), x: -DROP_SIZE / 2, y: drop.bobOffset - DROP_SIZE, w: DROP_SIZE, h: DROP_SIZE };
    }
    if (drop.kind === 'equipment') {
      const found = equipmentDropArt(drop, registry, classId);
      return found && { source: found.art, x: -found.w / 2, y: drop.bobOffset - 2 - found.h, w: found.w, h: found.h };
    }
    const sprite = registry.effects[drop.spriteKey];
    return sprite && { source: sprite, x: -sprite.width / 2, y: drop.bobOffset - sprite.height - 2, w: sprite.width, h: sprite.height };
  }

  update(world, registry) {
    const { env } = this;
    for (const drop of world.drops) {
      const art = this.art(drop, registry, world.player.classId);
      if (!art) continue;
      const node = this.set.get(drop);
      placeShadow(node.shadow, drop.x, drop.y, 3, 1.5, env.scale);
      placeSprite(node.body, env.tex.forSize(art.source, art.w, art.h, env.scale), drop.x + art.x, drop.y + art.y, art.w, art.h, env.scale);
    }
    this.set.endFrame();
  }

  clear() {
    this.painted.clear();
  }
}
