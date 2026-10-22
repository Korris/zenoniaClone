import { test } from 'node:test';
import assert from 'node:assert/strict';
import {
  canUpgradeSkill, upgradeSkill, canCastSkill, computePassiveModifiers, tickCooldowns,
  bindSkillToSlot, HOTBAR_SLOTS,
} from '../src/systems/skill-system.js';

const makePlayer = (overrides = {}) => ({
  level: 1, skillPoints: 1, skills: {}, mp: 40, hotbar: new Array(HOTBAR_SLOTS).fill(null), ...overrides,
});

test('vitality can be bought at level 1 with a point', () => {
  const player = makePlayer();
  assert.equal(canUpgradeSkill(player, 'vitality').ok, true);
  assert.equal(upgradeSkill(player, 'vitality').ok, true);
  assert.equal(player.skills.vitality, 1);
  assert.equal(player.skillPoints, 0);
});

test('upgrade refused without skill points', () => {
  const player = makePlayer({ skillPoints: 0 });
  assert.deepEqual(canUpgradeSkill(player, 'vitality'), { ok: false, reason: 'No skill points' });
});

test('upgrade refused below required hero level', () => {
  const player = makePlayer({ level: 1 });
  const check = canUpgradeSkill(player, 'slashWave');
  assert.equal(check.ok, false);
  assert.equal(check.reason, 'Requires Lv 2');
});

test('upgrade refused at max level', () => {
  const player = makePlayer({ level: 30, skillPoints: 10, skills: { vitality: 5 } });
  assert.equal(canUpgradeSkill(player, 'vitality').reason, 'Max level');
});

test('unknown skill is refused', () => {
  assert.equal(canUpgradeSkill(makePlayer(), 'nope').ok, false);
});

test('upgrade refused when a prerequisite is unmet, even if hero level is high enough', () => {
  const player = makePlayer({ level: 20, skillPoints: 5 });
  const check = canUpgradeSkill(player, 'whirlwind');
  assert.equal(check.ok, false);
  assert.equal(check.reason, 'Requires Slash Wave Lv 2');
});

test('whirlwind becomes available once Slash Wave reaches the required level', () => {
  const player = makePlayer({ level: 20, skillPoints: 5, skills: { slashWave: 2 } });
  assert.equal(canUpgradeSkill(player, 'whirlwind').ok, true);
});

test('a skill with two prerequisites needs both (Blade Storm)', () => {
  const player = makePlayer({ level: 30, skillPoints: 20, skills: { whirlwind: 3, heavyBlow: 0 } });
  assert.equal(canUpgradeSkill(player, 'bladeStorm').reason, 'Requires Heavy Blow Lv 1');
  player.skills.heavyBlow = 1;
  assert.equal(canUpgradeSkill(player, 'bladeStorm').ok, true);
});

test('learning an active skill auto-binds it to the first empty hotbar slot', () => {
  const player = makePlayer({ level: 20, skillPoints: 2 });
  upgradeSkill(player, 'slashWave');
  assert.equal(player.hotbar[0], 'slashWave');
  upgradeSkill(player, 'slashWave'); // second level of an already-bound skill doesn't rebind
  assert.equal(player.hotbar[0], 'slashWave');
  assert.equal(player.hotbar[1], null);
});

test('learning a passive skill never touches the hotbar', () => {
  const player = makePlayer();
  upgradeSkill(player, 'vitality');
  assert.deepEqual(player.hotbar, [null, null, null]);
});

test('bindSkillToSlot rejects passives, unlearned skills and out-of-range slots', () => {
  const player = makePlayer({ skills: { slashWave: 1 } });
  assert.equal(bindSkillToSlot(player, 'vitality', 0).ok, false);
  assert.equal(bindSkillToSlot(player, 'whirlwind', 0).reason, 'Not learned');
  assert.equal(bindSkillToSlot(player, 'slashWave', 9).ok, false);
  assert.equal(bindSkillToSlot(player, 'slashWave', 1).ok, true);
  assert.equal(player.hotbar[1], 'slashWave');
  assert.equal(bindSkillToSlot(player, null, 1).ok, true);
  assert.equal(player.hotbar[1], null);
});

test('canCastSkill checks learned, cooldown and MP', () => {
  const player = makePlayer({ level: 5, skills: { slashWave: 1 }, mp: 2 });
  assert.equal(canCastSkill(player, 'whirlwind', {}).reason, 'Not learned');
  assert.equal(canCastSkill(player, 'slashWave', { slashWave: 500 }).reason, 'Cooling down');
  assert.equal(canCastSkill(player, 'slashWave', {}).reason, 'Not enough MP');
  player.mp = 20;
  const ok = canCastSkill(player, 'slashWave', {});
  assert.equal(ok.ok, true);
  assert.equal(ok.cost, 6);
  assert.equal(canCastSkill(player, 'vitality', {}).ok, false);
});

test('computePassiveModifiers reflects vitality skill level (keeps original % values)', () => {
  assert.equal(computePassiveModifiers(makePlayer()).hpMult, 1);
  assert.ok(Math.abs(computePassiveModifiers(makePlayer({ skills: { vitality: 2 } })).hpMult - 1.2) < 1e-9);
});

test('computePassiveModifiers sums multiple stats from a single passive (Focus)', () => {
  const mods = computePassiveModifiers(makePlayer({ skills: { focus: 2 } }));
  assert.ok(Math.abs(mods.mpRegenMult - 1.5) < 1e-9); // base 1 + 0.5 at level 2
  assert.ok(Math.abs(mods.mpMult - 1.1) < 1e-9); // base 1 + 0.1 at level 2
});

test('computePassiveModifiers sums across multiple learned passives', () => {
  const mods = computePassiveModifiers(makePlayer({ skills: { vitality: 1, ironSkin: 1, powerStrike: 1 } }));
  assert.ok(Math.abs(mods.hpMult - 1.1) < 1e-9);
  assert.equal(mods.defAdd, 2);
  assert.ok(Math.abs(mods.comboMult - 0.05) < 1e-9);
});

test('tickCooldowns counts down and clamps at zero', () => {
  const cooldowns = { slashWave: 100, whirlwind: 0 };
  tickCooldowns(cooldowns, 60);
  assert.equal(cooldowns.slashWave, 40);
  tickCooldowns(cooldowns, 60);
  assert.equal(cooldowns.slashWave, 0);
  assert.equal(cooldowns.whirlwind, 0);
});
