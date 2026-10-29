// Composes the world frame: tiles -> drops -> y-sorted (props, NPCs, monsters, hero) ->
// projectiles/effects -> screen-space text (damage numbers, name tags).
import { setScreenTransform, setWorldTransform } from '../core/canvas-display.js';
import { drawTilemap } from './tilemap-renderer.js';
import { drawPlayer } from './player-renderer.js';
import { drawMonster } from './monster-renderer.js';
import { drawProp, drawNpc, drawNameTags } from './prop-renderer.js';
import { drawDrops, drawProjectiles, drawEffects, drawParticles, drawFloatingTexts } from './effects-renderer.js';
import { drawIceStorm } from './ice-storm-renderer.js';
import { drawHealingZone } from './healing-zone-renderer.js';
import { drawUltimate } from './ultimate-renderer.js';
import { companionVisible } from '../entities/companion-follow.js';
import { drawCompanion, updateCompanionState } from './companion-renderer.js';
import { drawMonsterStatuses } from './monster-status-renderer.js';

const CULL_MARGIN = 64;

/**
 * @param {CanvasRenderingContext2D} ctx raw canvas context (screen space)
 * @param {import('../game/world-state.js').WorldState} world
 * @param {object} registry sprite registry
 * @param {{npcMarkers?:Record<string,string>, nearbyNpc?:object}} view extra per-frame info from the controller
 */
export function renderWorld(ctx, world, registry, view = {}) {
  const { camera, player } = world;
  ctx.imageSmoothingEnabled = false;
  // 'high' re-filters every draw and costs >half the GPU frame; HD sprites go through the
  // pre-shrunk levels in smooth-sprite-draw.js instead, so 'low' keeps the same look.
  ctx.imageSmoothingQuality = 'low';
  setWorldTransform(ctx, camera);

  const backdrop = registry.backdrops?.[world.mapDef?.backdrop] || null;
  drawTilemap(ctx, world.tilemap, camera, registry, world.elapsedMs, backdrop);
  for (const effect of world.effects) if (effect.kind === 'healingZone' || effect.kind === 'venomZone') drawHealingZone(ctx, effect);
  for (const effect of world.effects) if (effect.kind === 'iceStorm') drawIceStorm(ctx, effect, registry.effects.iceStorm, true);
  for (const effect of world.effects) if (effect.kind === 'earthBreak' || effect.kind === 'rocketBarrage') drawUltimate(ctx, effect, registry.effects[effect.kind], true);
  drawDrops(ctx, world.drops, registry, player.classId);

  // Y-sort so things lower on screen draw in front (classic top-down depth)
  const visible = (e) => camera.isVisible(e.x - CULL_MARGIN, e.y - CULL_MARGIN, CULL_MARGIN * 2, CULL_MARGIN * 2);
  const layer = [];
  for (const p of world.props) if (visible(p)) layer.push({ y: p.y, kind: 'prop', e: p });
  for (const n of world.npcs) if (visible(n)) layer.push({ y: n.y, kind: 'npc', e: n });
  for (const m of world.monsters) if (visible(m)) layer.push({ y: m.y, kind: 'monster', e: m });
  layer.push({ y: player.y, kind: 'player', e: player });
  const companion = registry.companions?.[player.classId] && companionVisible(player) ? updateCompanionState(player, performance.now()) : null;
  if (companion) layer.push({ y: companion.y, kind: 'companion', e: companion }); // cleric spirit / mechanic robot, y-sorted like the hero
  layer.sort((a, b) => a.y - b.y);

  const markers = view.npcMarkers || {};
  for (const item of layer) {
    if (item.kind === 'prop') drawProp(ctx, item.e, registry);
    else if (item.kind === 'npc') drawNpc(ctx, item.e, registry, markers[item.e.id]);
    else if (item.kind === 'monster') drawMonster(ctx, item.e, registry);
    else if (item.kind === 'companion') drawCompanion(ctx, registry.companions[player.classId], item.e, player, registry.heroFrameUnits, performance.now());
    else drawPlayer(ctx, player, registry);
  }

  drawMonsterStatuses(ctx, world.monsters);
  drawProjectiles(ctx, world.projectiles, registry);
  drawEffects(ctx, world.effects, registry);
  drawParticles(ctx, world.particles, registry);

  setScreenTransform(ctx);
  drawNameTags(ctx, world, view.nearbyNpc || null, registry);
  drawFloatingTexts(ctx, world.texts, camera);
}
