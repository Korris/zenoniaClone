// What the PixiJS world scene does not draw yet, painted by the existing Canvas2D code onto the TRANSPARENT 2D canvas
// that sits above the WebGL canvas: healing / venom zones, Ice Storm, the Earth Break / Rocket Barrage ultimates, monster
// status pips, and the screen-space text (damage numbers, NPC name tags). They therefore appear over the actors
// instead of under them and are not bloomed -- see the parity checklist in docs/session-handoff.md.
import { setScreenTransform, setWorldTransform } from '../../core/canvas-display.js';
import { drawHealingZone } from '../healing-zone-renderer.js';
import { drawIceStorm } from '../ice-storm-renderer.js';
import { drawUltimate } from '../ultimate-renderer.js';
import { drawMonsterStatuses } from '../monster-status-renderer.js';
import { drawNameTags } from '../prop-renderer.js';
import { drawFloatingTexts } from '../effects-renderer.js';
import { drawText } from '../../ui/pixel-text.js';

/** @param {string} status one-line renderer status ("PIXI bloom ON fx ON") drawn top-right */
export function drawLegacyOverlay(ctx, world, registry, view, status) {
  const { camera } = world;
  ctx.imageSmoothingEnabled = false;
  ctx.imageSmoothingQuality = 'low';
  setWorldTransform(ctx, camera);
  for (const e of world.effects) {
    if (e.kind === 'healingZone' || e.kind === 'venomZone') drawHealingZone(ctx, e);
    else if (e.kind === 'iceStorm') { drawIceStorm(ctx, e, registry.effects.iceStorm, true); drawIceStorm(ctx, e, registry.effects.iceStorm); }
    else if (e.kind === 'earthBreak' || e.kind === 'rocketBarrage') { drawUltimate(ctx, e, registry.effects[e.kind], true); drawUltimate(ctx, e, registry.effects[e.kind]); }
  }
  drawMonsterStatuses(ctx, world.monsters);
  setScreenTransform(ctx);
  drawNameTags(ctx, world, view.nearbyNpc || null, registry);
  drawFloatingTexts(ctx, world.texts, camera);
  if (status) drawText(ctx, `${status}   [F7 bloom] [F8 fx] [F9 renderer]`, 480, 622, { size: 11, align: 'center', color: '#bfe8ff' });
}
