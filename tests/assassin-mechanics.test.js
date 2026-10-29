// Assassin Shadow + Agility mechanics: Shadow Step blink, Backstab-from-behind, Smoke Bomb (slow/blind/stealth),
// Evasion dodge through the hit roll, Phantom Assault, Shadow Clone echo, Adrenaline/Quick Reflexes speed,
// double-stab finisher, Blade Dance pulses.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { hitPlayer } from '../src/systems/combat-system.js';
import { performComboHit } from '../src/systems/combo-hit-system.js';
import { hasteFactor, attackSpeedFactor, tickStatuses } from '../src/systems/player-status.js';
import { applyComboRiders } from '../src/systems/assassin-combo-riders.js';
import { slowFactor, isBlinded } from '../src/systems/monster-status.js';
import { tickMonsterStatus } from '../src/systems/monster-status-tick.js';
import { SKILL_DEFINITIONS } from '../src/data/skill-definitions.js';
import { assassinWorld, foe, step, withRandom } from './helpers/assassin-world.js';

test('Shadow Step blinks behind the nearest foe (opposite its facing), strikes it and grants brief invulnerability', () => {
  const world = assassinWorld({ shadowStep: 1 }); const p = world.player;
  const near = foe(140, 100, 'down'), far = foe(260, 100);
  world.monsters.push(far, near);
  const mp = p.mp;
  assert.equal(tryCastSkill(world, 'shadowStep').ok, true);
  assert.deepEqual([p.x, p.y], [140, 100 - 12], 'behind a monster facing down = above it');
  assert.equal(p.dir, 'down', 'hero turns to face the target');
  assert.ok(near.hp < near.maxHp && far.hp === far.maxHp);
  assert.ok(p.invulnTimer >= 300);
  assert.equal(p.mp, mp - SKILL_DEFINITIONS.shadowStep.mpCost[0]);
  assert.ok(world.effects.length >= 2, 'departure + arrival puffs');
});

test('Shadow Step with no foe in range is refunded: MP back, no cooldown', () => {
  const world = assassinWorld({ shadowStep: 1 }); const p = world.player;
  world.monsters.push(foe(400, 100));
  const mp = p.mp;
  const r = tryCastSkill(world, 'shadowStep');
  assert.deepEqual([r.ok, r.reason], [false, 'No target']);
  assert.equal(p.mp, mp);
  assert.equal(p.cooldowns.shadowStep, 0);
  assert.equal(p.x, 100);
});

test('Backstab hits harder from behind than from the front, and never misses', () => {
  const dmg = (monsterDir) => withRandom(0.5, () => {
    const world = assassinWorld({ backstab: 1 });
    world.hitRoll = () => 0.999; // would MISS an ordinary hit
    const m = foe(118, 100, monsterDir);
    world.monsters.push(m);
    tryCastSkill(world, 'backstab');
    return m.maxHp - m.hp;
  });
  const behind = dmg('right'), front = dmg('left'); // hero stands left of the foe: facing right = back turned
  assert.ok(front > 0 && behind > front * 1.35, `behind ${behind} vs front ${front}`);
});

test('Backstab critBonus adds crit chance to that hit only', () => {
  const world = assassinWorld({ backstab: 5 });
  const m = foe(118, 100, 'left');
  world.monsters.push(m);
  withRandom(0.4, () => tryCastSkill(world, 'backstab')); // base crit 0, bonus 0.5 -> 0.4 < 0.5 crits
  const crit = world.texts.find((t) => t.damageStyle === 'outgoing');
  assert.equal(crit.critical, true);
  assert.equal(world.player.passiveMods.critChance, 0);
});

test('Evasion passive + Smoke Bomb stealth make ordinary hits MISS through the shared hit roll', () => {
  const world = assassinWorld({}); const p = world.player;
  world.hitRoll = () => 0.9; // between 95% and 70%
  p.invulnTimer = 0;
  hitPlayer(world, 5, 0, 0);
  assert.equal(p.hp < p.maxHp, true, 'no evasion: 90% roll < 95% hit chance = hit');

  const dodge = assassinWorld({ evasion: 5 }); // 20% -> 75% hit chance
  dodge.hitRoll = () => 0.9;
  hitPlayer(dodge, 5, 0, 0);
  assert.equal(dodge.player.hp, dodge.player.maxHp);
  assert.ok(dodge.texts.some((t) => t.text === 'MISS'));
  assert.equal(dodge.player.invulnTimer, 0, 'a dodged hit does not grant hit invulnerability');
  dodge.hitRoll = () => 0.6;
  hitPlayer(dodge, 5, 0, 0);
  assert.ok(dodge.player.hp < dodge.player.maxHp, 'a low roll still hits');

  const smoke = assassinWorld({ smokeBomb: 1 });
  tryCastSkill(smoke, 'smokeBomb');
  assert.equal(smoke.player.evasion, 0.25);
  smoke.hitRoll = () => 0.8; // 95% - 25% = 70%
  hitPlayer(smoke, 5, 0, 0, true);
  assert.equal(smoke.player.hp, smoke.player.maxHp, 'dodged while stealthed');
});

test('Smoke Bomb slows and blinds foes in the radius, stealths the hero, keeps the combo alive, all expire', () => {
  const world = assassinWorld({ smokeBomb: 1 }); const p = world.player;
  const inside = foe(120, 100), outside = foe(220, 100);
  world.monsters.push(inside, outside);
  p.combo.pressAttack();
  const comboStep = p.combo.step;
  tryCastSkill(world, 'smokeBomb');
  assert.equal(p.combo.step, comboStep, 'no damage -> combo not interrupted');
  assert.equal(slowFactor(inside), 0.6);
  assert.equal(isBlinded(inside), true);
  assert.equal(slowFactor(outside), 1);
  assert.equal(isBlinded(outside), false);
  assert.ok(p.stealth && p.stealth.timer === 2500);
  tickMonsterStatus(world, inside, 3100);
  assert.equal(slowFactor(inside), 1);
  assert.equal(isBlinded(inside), false);
  tickStatuses(p, 2600);
  assert.equal(p.stealth, null);
  assert.equal(p.evasion, 0);
});

test('Phantom Assault strikes up to `targets` foes in sequence (first instantly), invulnerable meanwhile', () => {
  const world = assassinWorld({ phantomAssault: 1 }); const p = world.player;
  const foes = [130, 150, 170, 190].map((x) => foe(x, 100));
  world.monsters.push(...foes);
  tryCastSkill(world, 'phantomAssault');
  assert.ok(foes[0].hp < foes[0].maxHp, 'first strike is instant');
  assert.equal(foes[1].hp, foes[1].maxHp);
  assert.ok(p.invulnTimer >= 3 * 130);
  step(world, 500);
  assert.ok(foes.slice(0, 3).every((m) => m.hp < m.maxHp));
  assert.equal(foes[3].hp, foes[3].maxHp, 'level 1 hits three targets');
  const empty = assassinWorld({ phantomAssault: 1 });
  assert.equal(tryCastSkill(empty, 'phantomAssault').reason, 'No target');
});

test('Shadow Clone echoes basic hits around itself while alive, then expires; recast replaces it', () => {
  const world = assassinWorld({ shadowClone: 1 }); const p = world.player;
  tryCastSkill(world, 'shadowClone');
  const clone = world.effects.find((e) => e.kind === 'shadowClone');
  assert.ok(clone && clone.mult === 0.5);
  clone.x = 100; clone.y = 160; // park the clone away from the hero, next to the second foe
  const hitFoe = foe(116, 100), cloneFoe = foe(100, 160);
  world.monsters.push(hitFoe, cloneFoe);
  p.combo.startHit(1);
  applyComboRiders(world, performComboHit(world, 1), 1);
  assert.ok(hitFoe.hp < hitFoe.maxHp);
  assert.ok(cloneFoe.hp < cloneFoe.maxHp, 'clone echoed the hit');
  const echo = cloneFoe.maxHp - cloneFoe.hp, direct = hitFoe.maxHp - hitFoe.hp;
  assert.ok(echo < direct, 'echo is weaker (cloneMult 0.5)');

  const miss = assassinWorld({ shadowClone: 1 });
  tryCastSkill(miss, 'shadowClone');
  const lonely = foe(100, 160); miss.monsters.push(lonely);
  miss.effects[0].x = 100; miss.effects[0].y = 160;
  applyComboRiders(miss, [], 1);
  assert.equal(lonely.hp, lonely.maxHp, 'no echo when the swing landed nothing');

  tryCastSkill(world, 'shadowClone'); // cooldown -> refused
  world.player.cooldowns = {};
  tryCastSkill(world, 'shadowClone');
  assert.equal(world.effects.filter((e) => e.kind === 'shadowClone' && !e.dead).length, 1);
  step(world, 5200);
  assert.equal(world.effects.some((e) => e.kind === 'shadowClone' && !e.dead), false);
});

test('Adrenaline hastes movement + attacks without breaking the combo; Quick Reflexes stacks attack speed', () => {
  const world = assassinWorld({ adrenaline: 2, quickReflexes: 3 }); const p = world.player;
  assert.ok(Math.abs(attackSpeedFactor(p) - 1.18) < 1e-9, 'passive alone');
  p.combo.pressAttack();
  const comboStep = p.combo.step;
  tryCastSkill(world, 'adrenaline');
  assert.equal(p.combo.step, comboStep);
  assert.equal(hasteFactor(p), 1.28);
  assert.ok(Math.abs(attackSpeedFactor(p) - 1.28 * 1.18) < 1e-9);
  p.tickBuffs(7100);
  assert.equal(hasteFactor(p), 1);
  assert.ok(p.speed > 92, 'Quick Reflexes also raises move speed');
});

test('finisher is a double stab: two hits 90ms apart that together exceed one plain finisher', () => {
  const world = assassinWorld(); const p = world.player;
  const m = foe(118, 100);
  world.monsters.push(m);
  p.combo.startHit(4);
  withRandom(0.5, () => performComboHit(world, 4));
  const first = m.maxHp - m.hp;
  assert.ok(first > 0);
  step(world, 60);
  assert.equal(m.maxHp - m.hp, first, 'second stab not yet');
  m.hitBySwing.clear();
  step(world, 60);
  const total = m.maxHp - m.hp;
  assert.ok(total > first * 1.5, `two stabs landed (${first} then ${total})`);
  const mult = p.combo.multiplierFor(4, p.level);
  assert.ok(total / p.atk > mult * 0.9, 'combined damage is about 1.2x the finisher multiplier');
});

test('Blade Dance spins three times around the hero for the full radius', () => {
  const world = assassinWorld({ bladeDance: 1 });
  const near = foe(115, 100), far = foe(200, 100);
  world.monsters.push(near, far);
  tryCastSkill(world, 'bladeDance');
  step(world, 400);
  assert.equal(world.texts.filter((t) => t.damageStyle === 'outgoing').length, 3);
  assert.equal(far.hp, far.maxHp);
});
