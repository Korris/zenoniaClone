// Assassin Venom mechanics: Poison Blade (charges on basic hits) + poison DoT ticking, Toxic Mastery, Venom
// Cloud zone, Cripple Strike slow, Death Mark detonation, and the monster status rules.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { damageMonster } from '../src/systems/combat-system.js';
import { performComboHit } from '../src/systems/combo-hit-system.js';
import { applyComboRiders } from '../src/systems/assassin-combo-riders.js';
import { tickStatuses } from '../src/systems/player-status.js';
import { applyPoison, applySlow, applyMark, slowFactor, poisonDamage } from '../src/systems/monster-status.js';
import { tickMonsterStatus } from '../src/systems/monster-status-tick.js';
import { Monster } from '../src/entities/monster.js';
import { assassinWorld, foe, step } from './helpers/assassin-world.js';

const swing = (world, n = 1) => { world.player.combo.startHit(n); applyComboRiders(world, performComboHit(world, n), n); };

test('Poison Blade: tryCast arms charges and keeps the combo; basic hits poison the foe they damage', () => {
  const world = assassinWorld({ poisonBlade: 1 }); const p = world.player;
  const m = foe(116, 100);
  world.monsters.push(m);
  p.combo.pressAttack();
  const comboStep = p.combo.step;
  tryCastSkill(world, 'poisonBlade');
  assert.equal(p.combo.step, comboStep);
  assert.equal(p.poisonBlade.charges, 5);
  p.combo.reset();
  swing(world);
  assert.ok(m.status.poison, 'poisoned by the basic hit');
  assert.equal(p.poisonBlade.charges, 4);
  const expected = poisonDamage(p.atk, 0.3, p);
  assert.equal(m.status.poison.dmg, expected);
  assert.equal(m.status.poison.timer, 5000);
});

test('Poison Blade stops when charges or time run out; other hits are not poisoned', () => {
  const world = assassinWorld({ poisonBlade: 1 }); const p = world.player;
  tryCastSkill(world, 'poisonBlade');
  p.poisonBlade.charges = 1;
  const a = foe(116, 100), b = foe(116, 104);
  world.monsters.push(a, b);
  swing(world);
  assert.equal(p.poisonBlade, null, 'last charge consumed');
  assert.equal([a, b].filter((m) => m.status?.poison).length, 1);
  tryCastSkill(world, 'poisonBlade');
  tickStatuses(p, 10100);
  assert.equal(p.poisonBlade, null, 'expires after 10s');
});

test('poison ticks once a second as true damage, expires on time, refresh keeps the tick rhythm', () => {
  const world = assassinWorld();
  const m = foe(116, 100);
  applyPoison(m, 10, 3000);
  tickMonsterStatus(world, m, 900);
  assert.equal(m.hp, m.maxHp);
  tickMonsterStatus(world, m, 100);
  assert.equal(m.maxHp - m.hp, 10);
  applyPoison(m, 6, 3000); // weaker re-application: refreshes duration, keeps the stronger damage + the tick phase
  assert.equal(m.status.poison.dmg, 10);
  assert.equal(m.status.poison.timer, 3000);
  tickMonsterStatus(world, m, 1000);
  assert.equal(m.maxHp - m.hp, 20);
  tickMonsterStatus(world, m, 2000);
  assert.equal(m.status.poison, undefined);
  assert.equal(m.maxHp - m.hp, 40);
  assert.ok(world.texts.some((t) => t.damageStyle === 'outgoing'));
});

test('poison can kill and grants kill rewards through the normal death path', () => {
  const world = assassinWorld();
  world.spawner = { scheduleRespawn() {} };
  const m = foe(116, 100, 'down', 5);
  world.monsters.push(m);
  applyPoison(m, 10, 3000);
  tickMonsterStatus(world, m, 1000);
  assert.equal(m.dead, true);
  assert.ok(world.events.some((e) => e.type === 'monsterKilled'));
  tickMonsterStatus(world, m, 5000); // no further ticks on a corpse
});

test('Toxic Mastery boosts poison damage', () => {
  const plain = assassinWorld(), master = assassinWorld({ toxicMastery: 5 });
  const a = poisonDamage(plain.player.atk, 0.5, plain.player), b = poisonDamage(master.player.atk, 0.5, master.player);
  assert.ok(Math.abs(b / a - 2) < 0.1, `${a} -> ${b}`);
});

test('Venom Cloud poisons only foes standing inside the zone, ticks until it expires', () => {
  const world = assassinWorld({ venomCloud: 1 }); const p = world.player;
  const inside = foe(134, 100), outside = foe(134, 190);
  world.monsters.push(inside, outside);
  tryCastSkill(world, 'venomCloud');
  const zone = world.effects.find((e) => e.kind === 'venomZone');
  assert.ok(zone && zone.radius === 28 && zone.x === p.x + 34);
  step(world, 600);
  assert.ok(inside.status?.poison);
  assert.equal(outside.status?.poison, undefined);
  inside.x += 200; // walks out; poison runs out on its own
  step(world, 6200);
  assert.equal(zone.dead, true);
  tickMonsterStatus(world, inside, 2000);
  assert.equal(inside.status.poison, undefined);
});

test('Cripple Strike damages and slows; slow expires; bosses only feel half of it', () => {
  const world = assassinWorld({ crippleStrike: 1 });
  const m = foe(116, 100);
  const boss = foe(116, 104); boss.isBoss = true;
  world.monsters.push(m, boss);
  tryCastSkill(world, 'crippleStrike');
  assert.ok(m.hp < m.maxHp);
  assert.equal(slowFactor(m), 0.7);
  assert.equal(slowFactor(boss), 0.85);
  tickMonsterStatus(world, m, 2600);
  assert.equal(slowFactor(m), 1);
  applySlow(m, 0.5, 1000); applySlow(m, 0.8, 500);
  assert.equal(slowFactor(m), 0.5, 'the stronger slow wins');
});

test('Death Mark banks a share of damage taken, then detonates once on expiry for base + stored', () => {
  const world = assassinWorld({ deathMark: 1 }); const p = world.player;
  const m = foe(130, 100, 'down', 100000);
  world.monsters.push(m);
  tryCastSkill(world, 'deathMark');
  assert.equal(m.status.mark.timer, 4000);
  assert.equal(m.status.mark.base, p.atk * 3.0);
  damageMonster(world, m, 1000);
  assert.equal(m.status.mark.stored, 300);
  const before = m.hp;
  tickMonsterStatus(world, m, 3900);
  assert.equal(m.hp, before, 'still ticking down');
  tickMonsterStatus(world, m, 200);
  assert.equal(before - m.hp, Math.round(p.atk * 3.0 + 300));
  assert.equal(m.status.mark, undefined);
  const after = m.hp;
  tickMonsterStatus(world, m, 5000);
  assert.equal(m.hp, after, 'detonates only once, its own blast is not banked');
  assert.ok(world.texts.some((t) => t.text === 'DEATH MARK'));
});

test('Death Mark with no foe in range is refunded; a mark on a monster that died does nothing', () => {
  const world = assassinWorld({ deathMark: 1 });
  const mp = world.player.mp;
  assert.equal(tryCastSkill(world, 'deathMark').reason, 'No target');
  assert.equal(world.player.mp, mp);
  const m = new Monster('slime', 130, 100);
  applyMark(m, { durationMs: 100, base: 50, storePct: 0.5 });
  m.dead = true;
  tickMonsterStatus(world, m, 500);
  assert.ok(m.status.mark, 'no detonation on a corpse');
});
