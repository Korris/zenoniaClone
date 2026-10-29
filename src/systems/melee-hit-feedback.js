// Combat game feel glue for melee swings: weapon trail at swing start, hit-stop + screen shake when a swing lands.
// Safe with partial worlds (unit-test stubs): every feel hook is optional.
import { COMBAT_FEEL } from '../data/combat-feel-config.js';
import { hitStopFor } from './combat-feel.js';
import { DIRECTION_VECTORS } from '../core/constants.js';

const DIR_ANGLE = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 };

/** Swept ribbon from the hero's facing direction; odd steps sweep one way, even steps back (left-right-left). */
export function startSwingTrail(world, player, step, hit, isFinisher, cfg = COMBAT_FEEL.trail) {
  if (!world.feel) return null;
  const shape = isFinisher ? cfg.finisher : cfg.normal;
  const facing = DIR_ANGLE[player.dir] ?? 0;
  const half = shape.sweep / 2;
  const sign = step % 2 === 1 ? 1 : -1;
  const v = DIRECTION_VECTORS[player.dir] || { x: 0, y: 0 };
  return world.feel.addTrail({
    follow: player, offsetX: v.x * 3, offsetY: -7 + v.y * 3,
    startAngle: facing - sign * half, endAngle: facing + sign * half,
    radius: (hit.reach || 30) * shape.radiusFactor + shape.radiusPad,
    width: shape.width,
    sweepMs: Math.max(60, hit.activeMs || 90),
    lifeMs: cfg.lifeMs, lead: cfg.leadFraction,
    colors: cfg.colors[player.classId] || cfg.colors.default,
  });
}

/** Called once per swing that damaged at least one monster (MISS-only swings never reach this). */
export function applyMeleeHitFeedback(world, { finisher = false, crit = false, kill = false }) {
  world.addHitStop?.(hitStopFor({ finisher, crit }));
  const t = COMBAT_FEEL.shake.trauma;
  const trauma = Math.max(finisher ? t.finisher : 0, crit ? t.crit : 0, kill ? t.kill : 0);
  if (trauma > 0) world.camera?.addTrauma?.(trauma);
}
