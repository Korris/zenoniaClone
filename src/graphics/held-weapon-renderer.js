// Paper-doll held gear: gear-less body frame + overlays (staff / sword / shield) drawn at the per-frame grip anchors
// (anchors from tools/estimate-*-anchors.py; the transform mirrors tools/held_gear_preview.py).
import { drawSmoothImage } from './smooth-sprite-draw.js';
import { bakeStaffGlow } from './held-weapon-glow-baker.js';
import { HELD_WEAPON_ENABLED, HELD_WEAPON_CLASSES, heldTierOf, heldBandOf } from './held-weapon-config.js';

const AURA_PULSE_MS = 900;
const FRAME_PX = 192;

/** {body, layers:[{id, anchor, baked, band}]} for a baked hero frame, or null => draw the baked frame as before. */
export function resolveHeldGear(registry, player, frame) {
  if (!HELD_WEAPON_ENABLED || !HELD_WEAPON_CLASSES.includes(player.classId)) return null;
  const gear = registry.heldGear?.[player.classId];
  const held = gear?.byFrame.get(frame);
  if (!held) return null;
  const layers = [];
  let body = held.bare;
  for (const part of gear.parts) {
    const item = player.equipment?.[part.slot] || null;
    const anchor = held.anchors[part.id];
    if (anchor?.fallback) return null; // overlay does not match the armed art in this frame: draw the baked frame
    if (!anchor) { // no overlay data for this part in this animation: keep the gear painted into the body if equipped
      if (part.bodyFallback && item && held.partial) body = held.partial;
      continue;
    }
    if (part.optional && !item) continue; // e.g. empty offhand: bare fist, no shield
    const overlays = gear.overlays[part.id];
    const overlay = overlays[heldTierOf(item)] || overlays[0];
    if (!overlay) continue;
    const band = heldBandOf(item);
    layers.push({ id: part.id, anchor, band, drawScale: part.drawScale || 1, minSx: part.minSx || 0, baked: bakeStaffGlow(band >= 1 ? overlay.enhanced : overlay.base, overlay.glow, band) });
  }
  return { body, layers };
}

/** Draws one overlay; (x, y) = top-left of the frame in world units, `size` = frame size in units. */
export function drawGear(ctx, layer, x, y, size, nowMs) {
  const { anchor, baked } = layer;
  const unit = size / FRAME_PX; // world units per frame pixel
  const k = anchor.s * unit * (layer.drawScale || 1); // world units per overlay pixel
  ctx.save();
  ctx.translate(x + anchor.x * unit, y + anchor.y * unit);
  ctx.rotate((anchor.angle * Math.PI) / 180);
  if (anchor.sx && anchor.sx !== 1) { // side views foreshorten the gear horizontally (optionally capped per part)
    const sx = Math.sign(anchor.sx) * Math.max(Math.abs(anchor.sx), layer.minSx || 0);
    ctx.scale(sx, 1);
  }
  const dx = -baked.gripX * k, dy = -baked.gripY * k;
  if (baked.aura) {
    ctx.globalAlpha = 0.775 + 0.225 * Math.sin((nowMs / AURA_PULSE_MS) * Math.PI * 2);
    drawSmoothImage(ctx, baked.aura, dx, dy, baked.aura.width * k, baked.aura.height * k);
    ctx.globalAlpha = 1;
  }
  drawSmoothImage(ctx, baked.body, dx, dy, baked.body.width * k, baked.body.height * k);
  ctx.restore();
}

/** Behind layers, body, then front layers. `drawBody(frame, x, y)` draws a hero frame the way player-renderer does. */
export function drawHeldHero(ctx, held, drawBody, x, y, size, nowMs) {
  const shown = held.layers.filter((l) => !l.anchor.hidden);
  for (const l of shown) if (l.anchor.behind) drawGear(ctx, l, x, y, size, nowMs);
  drawBody(held.body, x, y);
  for (const l of shown) if (!l.anchor.behind) drawGear(ctx, l, x, y, size, nowMs);
}
