// The y-sorted actor layer of the PixiJS scene: props, NPCs, monsters (incl. bosses), the hero + held gear, the
// floating companion and Assassin shadow clones. Every entity owns one retained node (RetainedSet over a NodePool),
// so a frame only re-points textures / transforms -- no display objects are created while playing. Depth is the
// container's zIndex (depth-key.js), which reproduces the Canvas2D renderer's y-sort.
import { getPropDefinition } from '../../world/prop-definitions.js';
import { companionVisible, COMPANION_HOVER, companionBob } from '../../entities/companion-follow.js';
import { npcFrame, npcFrameUnits } from '../prop-renderer.js';
import { updateCompanionState, resolveCompanionArt } from '../companion-renderer.js';
import { ActorNode, placeSprite, placeShadow } from './pixi-actor-node.js';
import { HeroNode, GearSlot } from './pixi-hero-node.js';
import { monsterSpec } from './pixi-monster-spec.js';
import { depthKey, snapToDevice } from './depth-key.js';
import { RetainedSet, NodePool } from './retained-set.js';

const CULL_MARGIN = 64;
const FRAME_PX = 192;

export class ActorLayer {
  constructor(env, container) {
    this.env = env;
    this.container = container;
    container.sortableChildren = true;
    this.props = this.actorSet('prop');
    this.npcs = this.actorSet('npc');
    this.monsters = this.actorSet('monster');
    this.hero = new HeroNode(env);
    this.companion = new GearSlot(env.PIXI);
    container.addChild(this.hero.root, this.companion.root);
    const heroPool = new NodePool(() => { const n = new HeroNode(env); container.addChild(n.root); return n; }, (n) => n.hide());
    this.clones = new RetainedSet((clone) => { const n = heroPool.take(); n.ghost = Object.create(clone.owner); return n; }, (n) => heroPool.put(n));
  }

  actorSet(kind) {
    const pool = new NodePool(() => { const n = new ActorNode(this.env, kind); this.container.addChild(n.root); return n; }, (n) => n.hide());
    return new RetainedSet(() => { const n = pool.take(); n.root.visible = true; return n; }, (n) => pool.put(n));
  }

  update(world, registry, view) {
    const { env } = this;
    const { camera, player } = world;
    const visible = (e) => camera.isVisible(e.x - CULL_MARGIN, e.y - CULL_MARGIN, CULL_MARGIN * 2, CULL_MARGIN * 2);
    for (const p of world.props) if (visible(p)) this.updateProp(p, registry);
    const markers = view.npcMarkers || {};
    for (const n of world.npcs) if (visible(n)) this.updateNpc(n, registry, markers[n.id]);
    for (const m of world.monsters) if (visible(m)) this.updateMonster(m, registry);
    this.hero.update(player, registry, env);
    for (const clone of world.effects) if (clone.kind === 'shadowClone') this.updateClone(clone, registry);
    this.updateCompanion(player, registry);
    for (const set of [this.props, this.npcs, this.monsters, this.clones]) set.endFrame();
  }

  updateProp(prop, registry) {
    const canvas = registry.props[prop.type];
    if (!canvas) return;
    const def = getPropDefinition(prop.type);
    const node = this.props.get(prop);
    const { env } = this;
    node.setDepth('prop', prop.y);
    placeSprite(node.body, env.tex.forSize(canvas, def.width, def.height, env.scale), Math.round(prop.x - def.width / 2), Math.round(prop.y - def.height), def.width, def.height, env.scale);
  }

  updateNpc(npc, registry, marker) {
    const { env } = this;
    const frame = npcFrame(npc, registry);
    const size = npcFrameUnits(npc, registry);
    const hd = (frame.pixelsPerUnit || 1) > 1;
    const node = this.npcs.get(npc);
    const x = npc.x - size / 2, y = npc.y - size;
    node.setDepth('npc', npc.y);
    placeShadow(node.shadow, npc.x, npc.y, hd ? 6 : 5, hd ? 2.5 : 2, env.scale);
    placeSprite(node.body, env.tex.forSize(frame, size, size, env.scale), x, y, size, size, env.scale);
    const badge = marker && registry.effects.markers?.[marker];
    node.badge.visible = Boolean(badge);
    if (!badge) return;
    const ppu = badge.pixelsPerUnit || 1, w = badge.width / ppu, h = badge.height / ppu;
    const float = Math.round(Math.sin(npc.animTimer / 250) * 1.5);
    placeSprite(node.badge, env.tex.forSize(badge, w, h, env.scale), npc.x - w / 2, snapToDevice(y, env.scale) - h - 2 + float, w, h, env.scale);
  }

  updateMonster(monster, registry) {
    const spec = monsterSpec(monster, registry);
    if (!spec) return;
    const { env } = this;
    const node = this.monsters.get(monster);
    const { rect, frame, rim } = spec;
    node.setDepth('monster', monster.y);
    placeShadow(node.shadow, monster.x, monster.y, spec.shadow.rx, spec.shadow.ry, env.scale);
    placeSprite(node.body, env.tex.forSize(frame, spec.baseW, spec.baseH, env.scale), rect.x, rect.y, rect.w, rect.h, env.scale, spec.flipX);
    node.rim.visible = Boolean(rim);
    if (rim) placeSprite(node.rim, env.tex.forSize(rim.frame, spec.baseW, spec.baseH, env.scale), rim.rect.x, rim.rect.y, rim.rect.w, rim.rect.h, env.scale);
    const ring = monster.isTelegraphing;
    node.telegraph.visible = ring;
    if (ring) {
      const r = Math.min(monster.def.attackRange, 16) + 4;
      node.telegraph.position.set(monster.x, monster.y);
      node.telegraph.scale.set((r * 2) / 64, r / 32);
      node.telegraph.alpha = 0.35 + 0.25 * Math.sin(monster.animTimer / 50);
    }
    node.markers.visible = spec.markers;
    if (spec.markers) node.showMarkers(monster, spec.markerTop, env.scale);
  }

  updateClone(clone, registry) {
    const node = this.clones.get(clone);
    node.ghost.x = clone.x;
    node.ghost.y = clone.y;
    node.root.visible = true;
    node.update(node.ghost, registry, this.env, 0.5 * clone.intensity);
  }

  updateCompanion(player, registry) {
    const companion = registry.companions?.[player.classId];
    const state = companion && companionVisible(player) ? updateCompanionState(player, performance.now()) : null;
    const art = state && resolveCompanionArt(companion, player.equipment?.offhand, player.dir, this.env.now);
    this.companion.root.visible = Boolean(art);
    if (!art) return;
    const unit = registry.heroFrameUnits / FRAME_PX;
    const lift = COMPANION_HOVER + companionBob(state.ageMs, art.animated ? 0.6 : 1.3);
    const anchor = { x: state.x / unit, y: (state.y - lift) / unit, angle: 0, s: companion.scale };
    this.companion.place({ anchor, baked: art.baked }, 0, 0, registry.heroFrameUnits, this.env);
    this.companion.root.alpha = 0.96;
    this.companion.root.zIndex = depthKey('companion', state.y);
  }
}
