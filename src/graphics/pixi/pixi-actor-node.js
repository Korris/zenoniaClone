// Retained display node for one prop / NPC / monster: a Container holding only the parts that kind needs
// (shadow, red telegraph ellipse, hit-flash rim, body, wind-up "!" + mini HP bar, NPC quest badge). Nodes are
// created once per entity by a RetainedSet and just re-pointed (texture / position / scale) each frame.
import { depthKey, snapToDevice } from './depth-key.js';

const PARTS = Object.freeze({
  prop: ['body'],
  drop: ['shadow', 'body'],
  npc: ['shadow', 'body', 'badge'],
  monster: ['shadow', 'telegraph', 'rim', 'body', 'markers'],
});

/** Point `sprite` at `tex` covering the world-unit rect (x, y, w, h), optionally mirrored; snapped to device pixels. */
export function placeSprite(sprite, tex, x, y, w, h, scale, flipX = false) {
  if (sprite.texture !== tex) sprite.texture = tex;
  sprite.scale.set((flipX ? -w : w) / tex.width, h / tex.height);
  sprite.position.set(snapToDevice(flipX ? x + w : x, scale), snapToDevice(y, scale));
}

export function placeShadow(sprite, x, y, rx, ry, scale) {
  sprite.visible = true;
  sprite.scale.set((rx * 2) / 64, (ry * 2) / 32);
  sprite.position.set(snapToDevice(x, scale), snapToDevice(y, scale));
}

function tintedSprite(PIXI, tex, tint, alpha = 1) {
  const s = new PIXI.Sprite(tex);
  s.tint = tint;
  s.alpha = alpha;
  return s;
}

export class ActorNode {
  /** @param {{PIXI:object, prim:object}} env */
  constructor(env, kind) {
    const { PIXI, prim } = env;
    this.kind = kind;
    this.root = new PIXI.Container();
    const parts = PARTS[kind];
    if (parts.includes('shadow')) { this.shadow = tintedSprite(PIXI, prim.ellipse, 0x000000, 0.3); this.shadow.anchor.set(0.5); this.root.addChild(this.shadow); }
    if (parts.includes('telegraph')) { this.telegraph = tintedSprite(PIXI, prim.ellipse, 0xff3c3c); this.telegraph.anchor.set(0.5); this.telegraph.visible = false; this.root.addChild(this.telegraph); }
    if (parts.includes('rim')) { this.rim = new PIXI.Sprite(); this.rim.visible = false; this.root.addChild(this.rim); }
    this.body = new PIXI.Sprite();
    this.root.addChild(this.body);
    if (parts.includes('markers')) {
      this.markers = new PIXI.Container();
      const white = prim.white;
      // [0,1] wind-up "!" bars, [2] bar frame, [3] bar track, [4] bar fill
      this.pieces = [0xff3b3b, 0xff3b3b, 0x1b1b2a, 0x3a3a4a, 0xe03c3c].map((c) => tintedSprite(PIXI, white, c));
      for (const piece of this.pieces) this.markers.addChild(piece);
      this.root.addChild(this.markers);
    }
    if (parts.includes('badge')) { this.badge = new PIXI.Sprite(); this.badge.visible = false; this.root.addChild(this.badge); }
  }

  setDepth(kind, y) {
    this.root.zIndex = depthKey(kind, y);
  }

  /** Wind-up "!" and mini HP bar above `topY`, mirroring monster-renderer.js drawMarkers. */
  showMarkers(monster, topY, scale) {
    const [bang1, bang2, frame, track, fill] = this.pieces;
    const snap = (v) => snapToDevice(v, scale);
    const cx = Math.round(monster.x), top = snap(topY);
    bang1.visible = bang2.visible = monster.isTelegraphing;
    if (monster.isTelegraphing) {
      bang1.position.set(cx - 1, top - 7); bang1.scale.set(2, 4);
      bang2.position.set(cx - 1, top - 2); bang2.scale.set(2, 1);
    }
    const hurt = monster.hp < monster.maxHp;
    frame.visible = track.visible = fill.visible = hurt;
    if (!hurt) return;
    const w = 14, x = Math.round(monster.x - w / 2), y = top - 4;
    frame.position.set(x - 1, y - 1); frame.scale.set(w + 2, 3);
    track.position.set(x, y); track.scale.set(w, 1);
    fill.position.set(x, y); fill.scale.set(Math.max(1, Math.round((w * monster.hp) / monster.maxHp)), 1);
  }

  /** Releasing a node from the scene: hidden until a new entity claims it. */
  hide() {
    this.root.visible = false;
  }
}
