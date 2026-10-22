// Composes the world frame: tiles -> drops -> y-sorted (props, NPCs, monsters, hero) ->
// projectiles/effects -> screen-space text (damage numbers, name tags).
import { PIXEL_SCALE } from '../core/constants.js';
import { drawTilemap } from './tilemap-renderer.js';
import { drawPlayer } from './player-renderer.js';
import { drawMonster } from './monster-renderer.js';
import { drawProp, drawNpc, drawNameTags } from './prop-renderer.js';
import { drawDrops, drawProjectiles, drawEffects, drawParticles, drawFloatingTexts } from './effects-renderer.js';

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
  ctx.setTransform(PIXEL_SCALE, 0, 0, PIXEL_SCALE, -camera.renderX * PIXEL_SCALE, -camera.renderY * PIXEL_SCALE);

  drawTilemap(ctx, world.tilemap, camera, registry, world.elapsedMs);
  drawDrops(ctx, world.drops, registry);

  // Y-sort so things lower on screen draw in front (classic top-down depth)
  const visible = (e) => camera.isVisible(e.x - CULL_MARGIN, e.y - CULL_MARGIN, CULL_MARGIN * 2, CULL_MARGIN * 2);
  const layer = [];
  for (const p of world.props) if (visible(p)) layer.push({ y: p.y, kind: 'prop', e: p });
  for (const n of world.npcs) if (visible(n)) layer.push({ y: n.y, kind: 'npc', e: n });
  for (const m of world.monsters) if (visible(m)) layer.push({ y: m.y, kind: 'monster', e: m });
  layer.push({ y: player.y, kind: 'player', e: player });
  layer.sort((a, b) => a.y - b.y);

  const markers = view.npcMarkers || {};
  for (const item of layer) {
    if (item.kind === 'prop') drawProp(ctx, item.e, registry);
    else if (item.kind === 'npc') drawNpc(ctx, item.e, registry, markers[item.e.id]);
    else if (item.kind === 'monster') drawMonster(ctx, item.e, registry);
    else drawPlayer(ctx, player, registry);
  }

  drawProjectiles(ctx, world.projectiles, registry);
  drawEffects(ctx, world.effects, registry);
  drawParticles(ctx, world.particles);

  ctx.setTransform(1, 0, 0, 1, 0, 0);
  drawNameTags(ctx, world, view.nearbyNpc || null, registry);
  drawFloatingTexts(ctx, world.texts, camera);
}
