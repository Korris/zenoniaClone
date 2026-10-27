import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { damageMonster, performSkillStrike, performRadialHit } from '../src/systems/combat-system.js';
import { rollPlayerDamage } from '../src/systems/player-damage.js';

test('critical damage uses the existing 1.6 multiplier', () => {
  const p = new Player(); p.atk = 100;
  p.passiveMods.critChance = 0;
  const normal = rollPlayerDamage(p, 0, 1, () => 0.5);
  p.passiveMods.critChance = 1;
  const critical = rollPlayerDamage(p, 0, 1, () => 0.5);
  assert.equal(normal.crit, false); assert.equal(critical.crit, true);
  assert.equal(critical.damage, normal.damage * 1.6);
});

function worldFor(monster, crit) {
  const player = new Player(); player.x = 0; player.y = 0; player.dir = 'right';
  player.passiveMods.critChance = crit ? 1 : 0;
  return { player, hitRoll: () => 0.5, monsters: [monster], addText() {}, addSparks() {}, addEffect() {} };
}

test('critical knockback doubles both axes without extending duration', () => {
  const normal = new Monster('slime', 12, 0), crit = new Monster('slime', 12, 0);
  damageMonster(worldFor(normal, false), normal, 1, 30, -40);
  damageMonster(worldFor(crit, true), crit, 1, 30, -40, '#fff44d', true);
  assert.equal(crit.knockback.vx, normal.knockback.vx * 2);
  assert.equal(crit.knockback.vy, normal.knockback.vy * 2);
  assert.equal(crit.knockback.timer, normal.knockback.timer);
});

for (const [name, strike] of [
  ['strike', w => performSkillStrike(w, 40, 40, 0.01, 60)],
  ['radial', w => performRadialHit(w, 40, 0.01, 60)],
]) {
  test(`${name} propagates critical state to doubled knockback`, () => {
    const a = new Monster('slime', 12, 0), b = new Monster('slime', 12, 0);
    strike(worldFor(a, false)); strike(worldFor(b, true));
    assert.equal(b.knockback.vx, a.knockback.vx * 2);
    assert.ok(a.knockback.vx > 0);
  });
}
