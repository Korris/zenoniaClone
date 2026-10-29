// Hero combo swings: hit every monster in the swing arc once, plus the Assassin finisher's extra stabs.
import { DIRECTION_VECTORS } from '../core/constants.js';
import { boxesOverlap } from './damage-formula.js';
import { attackBoxFor, damageMonster } from './combat-system.js';
import { rollPlayerDamage, CRIT_COLOR } from './player-damage.js';
import { SkillPulse } from '../entities/skill-pulse.js';

const DIR_ANGLE = { right: 0, down: Math.PI / 2, left: Math.PI, up: -Math.PI / 2 };

/**
 * Called when a combo swing becomes active: hit every monster in the arc once. A hit with `stabs: N`
 * (Assassin finisher) splits its multiplier over N quick stabs, 90ms apart. Returns the monsters
 * the first swing damaged (for combo riders such as Poison Blade / Shadow Clone).
 */
export function performComboHit(world, step) {
  const { player } = world;
  const hit = player.combo.currentHit, stabs = hit.stabs || 1;
  const landed = swingOnce(world, step, hit, stabs > 1 ? 1.2 / stabs : 1);
  if (stabs > 1) {
    const serial = player.combo.serial;
    const pulse = new SkillPulse({
      remaining: stabs - 1, intervalMs: 90,
      onPulse: () => {
        // Cancel the remaining stabs when the hero died or the combo was interrupted/replaced.
        if (player.dead || !player.combo.isAttacking || player.combo.serial !== serial) { pulse.dead = true; return; }
        swingOnce(world, step, hit, 1.2 / stabs);
      },
    });
    world.skillPulses.push(pulse);
  }
  return landed;
}

export function swingOnce(world, step, hit, share) {
  const { player } = world;
  const box = attackBoxFor(player, hit.reach, hit.width);
  const swingId = world.nextSwingId();
  // Power Strike passive adds a flat combo-damage bonus on top of the per-hit multiplier.
  const mult = player.combo.multiplierFor(step, player.level) * (1 + (player.passiveMods?.comboMult || 0)) * share;
  const v = DIRECTION_VECTORS[player.dir];
  const isFinisher = step === player.combo.config.hits.length;

  // Slash arc in front of the hero; alternate hits mirror the arc so the combo reads left-right-overhead
  const fxOffset = hit.reach / 2 + 4;
  world.addEffect({
    x: player.x + v.x * fxOffset, y: player.y - 6 + v.y * fxOffset, spriteKey: isFinisher ? 'arc' : 'slash',
    rotation: DIR_ANGLE[player.dir], flipY: step === 2, lifeMs: isFinisher ? 240 : 170,
    scale: hit.fxScale ?? (isFinisher ? 1.1 : 0.8), tint: hit.tint,
  });
  if (isFinisher) world.addEffect({ x: player.x, y: player.y - 8, spriteKey: 'arc', rotation: DIR_ANGLE[player.dir] + Math.PI, lifeMs: 260, scale: 1.0, spin: 9, follow: player, offsetY: -8 });

  const landed = [];
  for (const monster of world.monsters) {
    if (monster.dead || monster.hitBySwing.has(swingId) || !boxesOverlap(box, monster.box)) continue;
    monster.hitBySwing.add(swingId);
    const { damage, crit } = rollPlayerDamage(player, monster.def.def, mult);
    const previousHp = monster.hp;
    damageMonster(world, monster, damage, v.x * hit.knockback, v.y * hit.knockback, crit ? CRIT_COLOR : isFinisher ? '#ffb13b' : '#ffffff', crit, true);
    if (monster.hp < previousHp) landed.push(monster);
  }
  if (landed.length > 0 && isFinisher) {
    world.camera.shake(2.5, 140);
    world.hitStopMs = 60;
  } else if (landed.length > 0) {
    world.hitStopMs = 25;
  }
  return landed;
}
