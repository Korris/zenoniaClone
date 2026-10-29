// Cleric mechanics: heal, healing circle (HoT zone), purify, sacred shield, blessing/haste buffs,
// resurrection grace, holy blasts, angel's descent, Haste in the controller.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { Player } from '../src/entities/player.js';
import { Monster } from '../src/entities/monster.js';
import { WorldState } from '../src/game/world-state.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { hitPlayer } from '../src/systems/combat-system.js';
import { hasteFactor, applyShield, absorbWithShield, tickStatuses } from '../src/systems/player-status.js';
import { heroAttack } from '../src/systems/player-damage.js';
import { HealingZone } from '../src/entities/healing-zone.js';
import { SKILL_DEFINITIONS } from '../src/data/skill-definitions.js';

function clericWorld(skills = {}) {
  const player = new Player('cleric');
  Object.assign(player, { x: 100, y: 100, dir: 'right', level: 20 });
  player.skills = { ...skills };
  player.recomputeStats();
  player.hp = player.maxHp; player.mp = player.maxMp;
  const world = new WorldState(player, { shake() {} });
  world.hitRoll = () => 0.5;
  return world;
}
const step = (world, ms, dt = 50) => { for (let t = 0; t < ms; t += dt) { for (const e of world.effects) e.update(dt); for (const s of world.skillPulses) s.update(dt); } };

test('heal restores a percentage of max HP and spends MP / starts cooldown', () => {
  const world = clericWorld({ heal: 3 }); const p = world.player;
  p.hp = 10;
  const mp = p.mp;
  assert.equal(tryCastSkill(world, 'heal').ok, true);
  const expected = Math.round(p.maxHp * SKILL_DEFINITIONS.heal.healPercent[2] / 100);
  assert.equal(p.hp, 10 + expected);
  assert.equal(p.mp, mp - SKILL_DEFINITIONS.heal.mpCost[2]);
  assert.equal(tryCastSkill(world, 'heal').reason, 'Cooling down');
});

test('Healing Circle ticks heal-over-time only while the hero stands inside, then expires', () => {
  const world = clericWorld({ healingCircle: 1 }); const p = world.player;
  p.hp = 20;
  tryCastSkill(world, 'healingCircle');
  const zone = world.effects.find((e) => e.kind === 'healingZone');
  assert.ok(zone && zone.radius === SKILL_DEFINITIONS.healingCircle.radius[0]);
  const perTick = Math.round(p.maxHp * 0.04);
  step(world, 1000);
  assert.equal(p.hp, 20 + perTick);
  p.x += zone.radius + 30; // walk out
  step(world, 2000);
  assert.equal(p.hp, 20 + perTick, 'no healing outside the circle');
  p.x = zone.x;
  step(world, 3100);
  assert.equal(p.hp, 20 + perTick * 4); // ticks at 1s (earlier), 4s, 5s, 6s -> 3 more while back inside
  assert.equal(zone.dead, true);
});

test('HealingZone entity fires exactly durationMs/tickMs ticks', () => {
  let ticks = 0;
  const z = new HealingZone({ x: 0, y: 0, radius: 10, durationMs: 6000, tickMs: 1000, onTick: () => { ticks++; } });
  for (let t = 0; t < 7000; t += 16) z.update(16);
  assert.equal(ticks, 6);
  assert.equal(z.dead, true);
});

test('Purify heals a little, clears knockback and grants brief invulnerability', () => {
  const world = clericWorld({ purify: 2 }); const p = world.player;
  p.hp = 30; p.knockback = { vx: 90, vy: 0, timer: 100 }; p.hitFlashTimer = 200;
  assert.equal(tryCastSkill(world, 'purify').ok, true);
  assert.equal(p.knockback.timer, 0);
  assert.equal(p.hitFlashTimer, 0);
  assert.equal(p.invulnTimer, SKILL_DEFINITIONS.purify.invulnMs[1]);
  assert.equal(p.hp, 30 + Math.round(p.maxHp * 0.13));
});

test('Sacred Shield absorbs damage before HP, expires, and blocks the hurt reaction when fully absorbed', () => {
  const world = clericWorld({ sacredShield: 1 }); const p = world.player;
  tryCastSkill(world, 'sacredShield');
  const amount = Math.round(p.maxHp * 0.2);
  assert.equal(p.shield.amount, amount);
  hitPlayer(world, amount - 5, 0, 0, false);
  assert.equal(p.hp, p.maxHp);
  assert.equal(p.shield.amount, 5);
  assert.equal(p.knockback.timer, 0, 'fully absorbed hit does not knock back');
  p.invulnTimer = 0;
  hitPlayer(world, 20, 0, 0, false);
  assert.equal(p.hp, p.maxHp - 15); // 5 absorbed, 15 through
  assert.equal(p.shield, null);
  applyShield(p, 50, 1000);
  tickStatuses(p, 1100);
  assert.equal(p.shield, null, 'timed out');
  assert.equal(absorbWithShield(p, 10), 0);
});

test('Blessing of Might multiplies ATK for its duration; Haste Hymn scales speed and expires', () => {
  const world = clericWorld({ blessingOfMight: 2, hasteHymn: 2 }); const p = world.player;
  const base = heroAttack(p);
  tryCastSkill(world, 'blessingOfMight');
  assert.equal(heroAttack(p), base * SKILL_DEFINITIONS.blessingOfMight.atkMult[1]);
  assert.equal(p.buffs.timer, 12000);
  tryCastSkill(world, 'hasteHymn');
  assert.equal(hasteFactor(p), SKILL_DEFINITIONS.hasteHymn.hasteMult[1]);
  p.tickBuffs(8100);
  assert.equal(hasteFactor(p), 1);
  assert.equal(p.haste, null);
});

test('Support skills keep the combo alive; damage skills reset it', () => {
  const world = clericWorld({ heal: 1, smite: 1 }); const p = world.player;
  p.combo.pressAttack();
  const step0 = p.combo.step;
  assert.ok(step0 > 0);
  tryCastSkill(world, 'heal');
  assert.equal(p.combo.step, step0);
  tryCastSkill(world, 'smite');
  assert.equal(p.combo.step, 0);
});

test('Resurrection Grace: lethal hit leaves the hero alive once per cooldown', () => {
  const world = clericWorld({ resurrectionGrace: 1 }); const p = world.player;
  p.recomputeStats();
  p.hp = 5;
  hitPlayer(world, 999, 0, 0, false);
  assert.equal(p.dead, false);
  assert.equal(p.hp, Math.round(p.maxHp * 0.2));
  assert.equal(p.graceCooldown, 120000);
  assert.ok(p.invulnTimer >= 1500);
  assert.ok(world.texts.some((t) => String(t.text).includes('Resurrection Grace')));
  // on cooldown: next lethal hit kills
  p.invulnTimer = 0; p.hp = 5;
  hitPlayer(world, 999, 0, 0, false);
  assert.equal(p.dead, true);
  // recharge ticks down and revive resets
  p.revive();
  assert.equal(p.graceCooldown, 0);
  p.invulnTimer = 0; p.hp = 5;
  hitPlayer(world, 999, 0, 0, false);
  assert.equal(p.dead, false);
  tickStatuses(p, 60000);
  assert.equal(p.graceCooldown, 60000);
});

test('without Resurrection Grace a lethal hit kills (no regression for other classes)', () => {
  const world = clericWorld(); const p = world.player;
  hitPlayer(world, 9999, 0, 0, false);
  assert.equal(p.dead, true);
  const w = new Player('warrior'); w.hp = 1;
  assert.equal(w.takeDamage(50) > 0 && w.dead, true);
});

test('Divine Judgment lands after its delay on foes ahead; Heavenly Ray sweeps five strikes', () => {
  const world = clericWorld({ divineJudgment: 1, heavenlyRay: 1 });
  const near = new Monster('slime', 140, 106), far = new Monster('slime', 190, 106);
  world.monsters.push(near, far);
  const hp = near.hp;
  tryCastSkill(world, 'divineJudgment');
  assert.equal(near.hp, hp, 'delayed pillar has not landed yet');
  step(world, 400);
  assert.ok(near.hp < hp, 'foe in the pillar is hit');
  assert.equal(far.hp, far.maxHp);

  const w2 = clericWorld({ heavenlyRay: 1 });
  const targets = [135, 150, 170, 190].map((x) => new Monster('slime', x, 106));
  w2.monsters.push(...targets);
  tryCastSkill(w2, 'heavenlyRay');
  assert.ok(targets[0].hp < targets[0].maxHp, 'first strike is instant');
  step(w2, 900);
  assert.ok(targets.every((m) => m.hp < m.maxHp), 'beam reached every target');
});

test("Angel's Descent damages nearby foes and grants ATK buff, haste and shield together", () => {
  const world = clericWorld({ angelsDescent: 1 }); const p = world.player;
  const foe = new Monster('slime', 120, 106); world.monsters.push(foe);
  tryCastSkill(world, 'angelsDescent');
  assert.ok(foe.hp < foe.maxHp);
  assert.equal(p.buffs.atkMult, 1.25);
  assert.equal(hasteFactor(p), 1.1);
  assert.equal(p.shield.amount, Math.round(p.maxHp * 0.25));
});

test('unlearned or unaffordable cleric skills do not cast', () => {
  const world = clericWorld({ heal: 1 });
  assert.equal(tryCastSkill(world, 'sacredShield').reason, 'Not learned');
  world.player.mp = 0;
  assert.equal(tryCastSkill(world, 'heal').reason, 'Not enough MP');
});
