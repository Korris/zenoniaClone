// Draws static props (trees, houses...) and NPCs (with quest markers + name tags) in world space.
import { PIXEL_SCALE, SHOW_DEBUG_LABELS } from '../core/constants.js';
import { getPropDefinition } from '../world/prop-definitions.js';
import { CHARACTER_SIZE, HERO_FRAME_SIZE } from './sprite-registry.js';
import { drawShadow, characterFrame } from './player-renderer.js';
import { drawText } from '../ui/pixel-text.js';
import { drawSmoothImage } from './smooth-sprite-draw.js';

const NPC_IDLE_FRAME_MS = 260; // slow, subtle breathing loop for the 4-frame HD idle rows
const HD_NPC_FACING = Object.freeze({ down: 'down', up: 'down', left: 'left', right: 'right' }); // no rear pose painted

export function drawProp(ctx, prop, registry) {
  const canvas = registry.props[prop.type];
  if (!canvas) return;
  const def = getPropDefinition(prop.type);
  // Props sit on their footprint: bottom of the canvas = bottom of the footprint tiles. Dest size
  // is always the def's world-unit box (never the source canvas's natural pixel size) so an HD
  // canvas (pixelsPerUnit 4) draws at the SAME world-space footprint as the pixel-art cut, just
  // crisper -- same convention as player-renderer.js/tilemap-renderer.js.
  const hd = (canvas.pixelsPerUnit || 1) > 1;
  if (hd) ctx.imageSmoothingEnabled = true;
  ctx.drawImage(canvas, Math.round(prop.x - def.width / 2), Math.round(prop.y - def.height), def.width, def.height);
  if (hd) ctx.imageSmoothingEnabled = false;
}

/** World units an NPC's sprite spans: HD villagers stand at hero height, pack NPCs at 16. */
export function npcFrameUnits(npc, registry) {
  return registry.npcsHd?.[npc.id] ? HERO_FRAME_SIZE : CHARACTER_SIZE;
}

/** Current frame for an NPC: HD idle loop (driven by npc.animTimer, never reset while standing)
 * when its sheet loaded, else the pixel-art pack frame. */
export function npcFrame(npc, registry) {
  const hd = registry.npcsHd?.[npc.id];
  if (hd) {
    const idle = hd[HD_NPC_FACING[npc.dir] || 'down'].idle;
    return idle[Math.floor(npc.animTimer / NPC_IDLE_FRAME_MS) % idle.length];
  }
  return characterFrame(registry.npcs[npc.def.sprite], npc.dir); // every def.sprite is in NPC_SHEETS (tested)
}

export function drawNpc(ctx, npc, registry, marker) {
  const frame = npcFrame(npc, registry);
  const size = npcFrameUnits(npc, registry);
  const hd = (frame.pixelsPerUnit || 1) > 1;
  const transform = ctx.getTransform();
  const drawX = (Math.round((npc.x - size / 2) * transform.a + transform.e) - transform.e) / transform.a;
  const drawY = (Math.round((npc.y - size) * transform.d + transform.f) - transform.f) / transform.d;
  drawShadow(ctx, npc.x, npc.y, hd ? 6 : 5, hd ? 2.5 : 2);
  ctx.imageSmoothingEnabled = hd;
  if (hd) drawSmoothImage(ctx, frame, drawX, drawY, size, size);
  else ctx.drawImage(frame, drawX, drawY, size, size);
  ctx.imageSmoothingEnabled = false;
  if (marker) {
    const sprite = registry.effects.markers[marker];
    if (sprite) {
      const float = Math.round(Math.sin(npc.animTimer / 250) * 1.5);
      const ppu = sprite.pixelsPerUnit || 1, w = sprite.width / ppu, h = sprite.height / ppu;
      ctx.imageSmoothingEnabled = ppu > 1;
      ctx.drawImage(sprite, npc.x - w / 2, drawY - h - 2 + float, w, h);
    }
  }
}

/** Name tags are drawn in screen space (after resetting the transform) so text stays crisp. The
 * hero's own label is a development overlay (SHOW_DEBUG_LABELS / window.__zenoniaDebug). */
export function drawNameTags(ctx, world, nearbyNpc, registry) {
  const { camera, player } = world;
  const tags = [];
  if (SHOW_DEBUG_LABELS || globalThis.__zenoniaDebug) tags.push({ x: player.x, y: player.y - HERO_FRAME_SIZE - 2, text: 'Hero', color: '#ffe680' });
  if (nearbyNpc) tags.push({ x: nearbyNpc.x, y: nearbyNpc.y - npcFrameUnits(nearbyNpc, registry) - 14, text: nearbyNpc.name, color: '#ffffff' });
  for (const tag of tags) {
    const sx = (tag.x - camera.renderX) * PIXEL_SCALE;
    const sy = (tag.y - camera.renderY) * PIXEL_SCALE;
    ctx.font = 'bold 12px "Courier New", monospace';
    const w = ctx.measureText(tag.text).width + 10;
    ctx.fillStyle = 'rgba(0,0,0,0.6)';
    ctx.fillRect(sx - w / 2, sy - 8, w, 16);
    drawText(ctx, tag.text, sx, sy - 7, { size: 12, align: 'center', color: tag.color, shadow: false });
  }
  if (nearbyNpc) {
    const sx = (nearbyNpc.x - camera.renderX) * PIXEL_SCALE;
    const sy = (nearbyNpc.y - camera.renderY) * PIXEL_SCALE;
    drawText(ctx, '[E] Talk', sx, sy + 8, { size: 11, align: 'center', color: '#8fff8f' });
  }
}
