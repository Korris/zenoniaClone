// Draws the combat game feel layer in world space: weapon trail ribbons, spark streaks, shock rings and
// impact stars. Plain path fills/strokes, no shadowBlur, no per-frame canvas creation. Two passes: saturated
// class-coloured edges in normal composite (additive colour vanishes on bright snow/sand), then white cores
// with the additive 'lighter' composite for the glow-like punch.
import { COMBAT_FEEL } from '../data/combat-feel-config.js';

/**
 * Outline of a trail as two arcs (outer = tip, inner = tip - width) sampled between the visible tail and head.
 * Width tapers to 0 toward the tail. Returns a flat [x,y,...] array, or null when nothing is visible.
 */
export function trailPolygon(trail, pivotX, pivotY, widthScale = 1, segments = COMBAT_FEEL.trail.segments) {
  const head = trail.head, tail = trail.tail;
  if (head <= tail) return null;
  const outer = [], inner = [];
  for (let i = 0; i <= segments; i++) {
    const f = tail + ((head - tail) * i) / segments; // arc fraction
    const age = trail.ageMs - f * trail.sweepMs; // how long ago the tip passed this spot
    const k = Math.max(0, 1 - age / trail.lifeMs);
    const angle = trail.startAngle + (trail.endAngle - trail.startAngle) * f;
    const c = Math.cos(angle), s = Math.sin(angle);
    const w = trail.width * widthScale * k * Math.min(1, 0.25 + f * 4); // fresh tip thick, very start thin
    outer.push(pivotX + c * trail.radius, pivotY + s * trail.radius);
    inner.push(pivotX + c * (trail.radius - w), pivotY + s * (trail.radius - w));
  }
  const pts = outer.slice();
  for (let i = inner.length - 2; i >= 0; i -= 2) pts.push(inner[i], inner[i + 1]);
  return pts;
}

function fillPolygon(ctx, pts, color, alpha) {
  ctx.globalAlpha = alpha;
  ctx.fillStyle = color;
  ctx.beginPath();
  ctx.moveTo(pts[0], pts[1]);
  for (let i = 2; i < pts.length; i += 2) ctx.lineTo(pts[i], pts[i + 1]);
  ctx.closePath();
  ctx.fill();
}

function drawTrail(ctx, trail, cfg, core) {
  const px = trail.follow.x + trail.offsetX, py = trail.follow.y + trail.offsetY;
  const poly = trailPolygon(trail, px, py, core ? 0.42 : 1);
  if (poly) fillPolygon(ctx, poly, core ? trail.colors.core : trail.colors.edge, core ? cfg.coreAlpha : cfg.edgeAlpha);
}

function starPath(ctx, r) {
  ctx.beginPath(); // 4-point star: long spikes on the axes, short between
  for (let i = 0; i < 8; i++) {
    const a = (i * Math.PI) / 4, rad = i % 2 === 0 ? r : r * 0.24;
    ctx.lineTo(Math.cos(a) * rad, Math.sin(a) * rad);
  }
  ctx.closePath();
}

function drawStar(ctx, star, colors, core) {
  const t = star.ageMs / star.lifeMs;
  const r = star.size * (t < 0.3 ? 0.5 + (t / 0.3) * 0.7 : 1.2 * (1 - (t - 0.3) / 0.7)); // pop then vanish
  if (r <= 0.2) return;
  ctx.save();
  ctx.translate(star.x, star.y);
  ctx.rotate(star.rotation);
  ctx.globalAlpha = 1;
  ctx.fillStyle = core ? colors.core : star.crit ? colors.crit : colors.edge;
  starPath(ctx, core ? r * 0.55 : r); ctx.fill();
  ctx.restore();
}

function drawStreaks(ctx, streaks, cfg, core) {
  const { colors, sparkLength } = cfg;
  // One batched path per pass; length shrinks as the spark ages instead of fading.
  ctx.beginPath();
  for (const s of streaks.items) {
    if (!s.active) continue;
    const life = 1 - s.ageMs / s.lifeMs;
    const speed = Math.hypot(s.vx, s.vy) || 1;
    const len = speed * sparkLength * (0.35 + 0.65 * life) + 1.2;
    ctx.moveTo(s.x, s.y);
    ctx.lineTo(s.x - (s.vx / speed) * len, s.y - (s.vy / speed) * len);
  }
  ctx.strokeStyle = core ? colors.core : colors.edge;
  ctx.lineWidth = core ? 0.5 : 1.2;
  ctx.globalAlpha = 1;
  ctx.stroke();
}

function drawRings(ctx, rings, colors, core) {
  for (const ring of rings.items) {
    if (!ring.active) continue;
    const t = ring.ageMs / ring.lifeMs;
    const ease = 1 - (1 - t) * (1 - t); // fast out, slow settle
    ctx.strokeStyle = core ? colors.core : ring.crit ? colors.crit : colors.edge;
    ctx.globalAlpha = 1 - t * t;
    ctx.lineWidth = Math.max(0.3, (core ? 1 : 2.4) * (1 - t));
    ctx.beginPath();
    ctx.arc(ring.x, ring.y, 2 + ring.maxRadius * ease, 0, Math.PI * 2);
    ctx.stroke();
  }
}

/** Edge pass (normal composite) then core pass ('lighter'); each layers trail -> ring -> sparks -> star. */
export function drawCombatFeel(ctx, feel) {
  if (!feel) return;
  const hasImpacts = feel.streaks.activeCount + feel.rings.activeCount + feel.stars.activeCount > 0;
  if (feel.trails.length === 0 && !hasImpacts) return;
  const { trail, impact } = COMBAT_FEEL;
  ctx.save();
  ctx.lineCap = 'round';
  ctx.lineJoin = 'round';
  for (const core of [false, true]) {
    ctx.globalCompositeOperation = core ? 'lighter' : 'source-over';
    for (const t of feel.trails) drawTrail(ctx, t, trail, core);
    drawRings(ctx, feel.rings, impact.colors, core);
    drawStreaks(ctx, feel.streaks, impact, core);
    for (const star of feel.stars.items) if (star.active) drawStar(ctx, star, impact.colors, core);
  }
  ctx.restore();
}
