// Companion art + drawing, shared by the cleric spirit and the mechanic robot (specs in companion-specs.js). Art
// (tools/cut-cleric-gear-overlays.py, tools/cut-mechanic-gear-overlays.py):
//  - 10 tier overlays (front view), base + enhanced (+7 and up) with the same baked glow bands as held gear;
//  - tier 0 base only: a 4-facing x 4-frame float loop used when the companion is unenhanced.
// Other tiers have no per-facing art, so they show the static tier art with the code bob (companionBob) only.
import { COMPANION_SPECS } from './companion-specs.js';
import { buildOverlayList } from './held-overlay-builder.js';
import { bakeStaffGlow } from './held-weapon-glow-baker.js';
import { drawGear } from './held-weapon-renderer.js';
import { heldTierOf, heldBandOf } from './held-weapon-config.js';
import { COMPANION_HOVER, companionBob, createCompanionState, stepCompanion } from '../entities/companion-follow.js';

const FRAME_PX = 192; // hero frame px per world hero frame (drawGear anchors are in these units)
const FLOAT_FRAME_MS = 140;

/** @returns {{overlays: object[], float: object, scale: number}|null} null when the art did not load (nothing drawn) */
export function buildCompanion(images, spec) {
  const overlays = buildOverlayList(images, spec.manifest, spec.imageKey);
  if (!overlays[0]) return null;
  const float = {};
  for (const [dir, list] of Object.entries(spec.float)) {
    const frames = list.map((f, i) => ({ img: images[spec.floatKey(dir, i)], gripX: f.gripX, gripY: f.gripY }));
    if (frames.every((f) => f.img)) float[dir] = frames;
  }
  return { overlays, float, scale: spec.scale };
}

/** Companions per class id; a class without loaded art is simply absent. */
export function buildCompanions(images) {
  const out = {};
  for (const [classId, spec] of Object.entries(COMPANION_SPECS)) {
    const companion = buildCompanion(images, spec);
    if (companion) out[classId] = companion;
  }
  return out;
}

/** Which art the equipped offhand selects: tier 0..9 (equipmentTier), glow band 0..4 (+7/9/11/13), enhanced art from band 1. */
export function companionVariant(offhand) {
  const band = heldBandOf(offhand);
  return { tier: heldTierOf(offhand), band, enhanced: band >= 1 };
}

/** Art for this frame: { baked: {body, aura, gripX, gripY}, band, animated } or null (companion art not loaded). */
export function resolveCompanionArt(companion, offhand, dir, nowMs) {
  if (!companion) return null;
  const { tier, band, enhanced } = companionVariant(offhand);
  const overlay = companion.overlays[tier] || companion.overlays[0];
  const frames = companion.float[dir] || companion.float.down;
  if (tier === 0 && !enhanced && frames) { // tier-1 base companion: the real float loop, facing the hero's direction
    const f = frames[Math.floor(nowMs / FLOAT_FRAME_MS) % frames.length];
    return { baked: { body: f.img, aura: null, gripX: f.gripX, gripY: f.gripY }, band: 0, animated: true };
  }
  return { baked: bakeStaffGlow(enhanced ? overlay.enhanced : overlay.base, overlay.glow, band), band, animated: false };
}

/** Draws the companion with its body centre at the state's ground point lifted by the hover height + bob. */
export function drawCompanion(ctx, companion, state, player, heroFrameUnits, nowMs) {
  const art = resolveCompanionArt(companion, player.equipment?.offhand, player.dir, nowMs);
  if (!art) return;
  const unit = heroFrameUnits / FRAME_PX; // world units per frame px
  const lift = COMPANION_HOVER + companionBob(state.ageMs, art.animated ? 0.6 : 1.3);
  const anchor = { x: state.x / unit, y: (state.y - lift) / unit, angle: 0, s: companion.scale };
  ctx.save();
  ctx.globalAlpha *= 0.96;
  drawGear(ctx, { anchor, baked: art.baked }, 0, 0, heroFrameUnits, nowMs);
  ctx.restore();
}

const states = new WeakMap(); // player -> { x, y, ageMs, lastMs }

/** Follow state for this player, advanced to `nowMs` (render clock). The first call places the companion at its spot. */
export function updateCompanionState(player, nowMs) {
  let state = states.get(player);
  if (!state) {
    state = { ...createCompanionState(player), lastMs: nowMs };
    states.set(player, state);
    return state;
  }
  stepCompanion(state, player, nowMs - state.lastMs);
  state.lastMs = nowMs;
  return state;
}
