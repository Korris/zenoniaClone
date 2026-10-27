import { UltimateEffect } from '../entities/ultimate-effect.js';
import { getSkillValue } from '../data/skill-definitions.js';
import { DIRECTION_VECTORS } from '../core/constants.js';
import { performRadialHit } from './combat-system.js';
import { playAttackSound } from '../audio/combat-audio.js';

export function castUltimate(world, level, mult, def) {
  const p = world.player, v = DIRECTION_VECTORS[p.dir], kind = def.effect.kind;
  const radius = getSkillValue(def.id, 'radius', level);
  const range = getSkillValue(def.id, 'range', level) || 32;
  const center = { x: p.x + v.x * range * (kind === 'earthBreak' ? 1 : 0.78), y: p.y - 6 + v.y * range * (kind === 'earthBreak' ? 1 : 0.78) };
  const effect = new UltimateEffect({ kind, caster: p, ...center, radius, dir: p.dir,
    count: getSkillValue(def.id, 'rocketCount', level) || 8, range,
    onEvent: event => {
      if (event.type === 'rocket') {
        performRadialHit(world, radius, mult, 35, event.rocket);
        world.camera.shake(1.4, 100);
      } else if (event.type === 'sweep') {
        playAttackSound(p.classId, 4, true);
        performRadialHit(world, radius * 0.75, mult * 0.2, 20, center);
      } else if (event.type === 'slam') {
        const connected = performRadialHit(world, radius, mult, 0, center);
        for (const m of connected) if (!m.dead && !m.isBoss) {
          m.airborneMs = 1500; m.knockback.timer = 0; m.ai.state = 'cooldown'; m.ai.timer = 0;
        }
        world.camera.shake(4, 220); world.hitStopMs = 70;
      } else performRadialHit(world, radius, mult * 0.12, 0, center);
    } });
  p.ultimateCast = effect; p.moving = false;
  world.effects.push(effect);
}
