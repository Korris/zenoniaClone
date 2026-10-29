// Data sanity + integration tests for the skill trees: 15 nodes per class (3 branches x 5 nodes).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { SKILL_DEFINITIONS, SKILL_ORDER, HOTBAR_SLOT_KEYS, getSkillValue } from '../src/data/skill-definitions.js';
import { CLASS_DEFINITIONS, CLASS_IDS, classForBranch } from '../src/data/class-definitions.js';
import { ASSET_FILES } from '../src/graphics/asset-atlas.js';
import { canUpgradeSkill, upgradeSkill, bindSkillToSlot, computePassiveModifiers, HOTBAR_SLOTS } from '../src/systems/skill-system.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';

const makePlayer = (overrides = {}) => ({
  level: 30, skillPoints: 20, skills: {}, mp: 999, hotbar: new Array(HOTBAR_SLOTS).fill(null),
  cooldowns: {}, dir: 'down', atk: 20, combo: { reset() {} }, ...overrides,
});

test('15 nodes per class, 5 per branch (2+2+1 by tier), 3 tiers, maxLevel 5 everywhere', () => {
  assert.equal(SKILL_ORDER.length, 15 * CLASS_IDS.length);
  assert.equal(new Set(SKILL_ORDER).size, SKILL_ORDER.length, 'skill ids must be unique across classes');
  for (const classId of CLASS_IDS) {
    for (const branch of CLASS_DEFINITIONS[classId].branches) {
      const nodes = SKILL_ORDER.filter((id) => SKILL_DEFINITIONS[id].branch === branch);
      assert.equal(nodes.length, 5, `${branch} should have 5 nodes`);
      assert.deepEqual(nodes.map((id) => SKILL_DEFINITIONS[id].tier), [1, 1, 2, 2, 3], `${branch} tier layout`);
    }
  }
  for (const id of SKILL_ORDER) {
    assert.ok(classForBranch(SKILL_DEFINITIONS[id].branch), `${id} belongs to no class`);
    for (const req of SKILL_DEFINITIONS[id].requires) {
      assert.equal(classForBranch(SKILL_DEFINITIONS[req.skill].branch), classForBranch(SKILL_DEFINITIONS[id].branch), `${id} requires a skill of another class`);
    }
  }
  for (const id of SKILL_ORDER) {
    const def = SKILL_DEFINITIONS[id];
    assert.equal(def.maxLevel, 5, `${id} maxLevel`);
    assert.equal(def.requiredLevel.length, 5, `${id} requiredLevel length`);
    assert.ok([1, 2, 3].includes(def.tier), `${id} tier`);
    assert.ok(def.type === 'active' || def.type === 'passive', `${id} type`);
  }
});

test('every prerequisite references a real skill id', () => {
  for (const id of SKILL_ORDER) {
    for (const req of SKILL_DEFINITIONS[id].requires || []) {
      assert.ok(SKILL_DEFINITIONS[req.skill], `${id} requires unknown skill ${req.skill}`);
      assert.ok(req.level >= 1 && req.level <= 5, `${id} requires valid level for ${req.skill}`);
    }
  }
});

test('every skill icon is registered in the asset manifest', () => {
  for (const id of SKILL_ORDER) {
    const icon = SKILL_DEFINITIONS[id].icon;
    assert.ok(icon, `${id} missing icon field`);
    assert.ok(ASSET_FILES[icon], `${id} icon "${icon}" not in ASSET_FILES`);
  }
});

test('active skills carry a valid effect.kind; passives carry a passive stat map', () => {
  const kinds = new Set(['projectile', 'radial', 'strike', 'dash', 'heal', 'buff', 'iceStorm', 'earthBreak', 'rocketBarrage', 'holyBlast', 'healZone', 'purify', 'shield', 'haste', 'angelsDescent',
    'shadowStep', 'backstab', 'smokeBomb', 'phantomAssault', 'shadowClone', 'poisonBlade', 'venomCloud', 'crippleStrike', 'deathMark']);
  for (const id of SKILL_ORDER) {
    const def = SKILL_DEFINITIONS[id];
    if (def.type === 'active') {
      assert.ok(def.effect && kinds.has(def.effect.kind), `${id} effect.kind`);
    } else {
      assert.ok(def.passive && Object.keys(def.passive).length > 0, `${id} passive stats`);
      for (const values of Object.values(def.passive)) assert.equal(values.length, 5, `${id} passive array length`);
    }
  }
});

test('hotbar has 3 slots matching K/L/I', () => {
  assert.equal(HOTBAR_SLOTS, 3);
  assert.deepEqual(HOTBAR_SLOT_KEYS, ['K', 'L', 'I']);
});

test('prerequisite gating: Blade Storm needs both Whirlwind 3 and Heavy Blow 1', () => {
  const player = makePlayer({ skills: { slashWave: 2, whirlwind: 2, powerStrike: 2, heavyBlow: 1 } });
  assert.equal(canUpgradeSkill(player, 'bladeStorm').ok, false); // whirlwind only at 2, needs 3
  player.skills.whirlwind = 3;
  assert.equal(canUpgradeSkill(player, 'bladeStorm').ok, true);
});

test('passive aggregation: Iron Skin + Swift Feet stack additively', () => {
  const player = makePlayer({ skills: { ironSkin: 2, swiftFeet: 3 } });
  const mods = computePassiveModifiers(player);
  assert.equal(mods.defAdd, 4);
  assert.ok(Math.abs(mods.speedMult - 1.12) < 1e-9);
});

test('hotbar binding: casting reads whatever skill is bound to the pressed slot', () => {
  const player = makePlayer({ skills: { slashWave: 1 } });
  bindSkillToSlot(player, 'slashWave', 2);
  assert.equal(player.hotbar[2], 'slashWave');
  const world = { player, monsters: [], projectiles: [], effects: [], skillPulses: [], camera: { shake() {} }, addText() {}, addEffect(o) { world.effects.push(o); } };
  const result = tryCastSkill(world, player.hotbar[2]);
  assert.equal(result.ok, true);
  assert.equal(world.projectiles.length, 1);
});

test('upgradeSkill auto-binds then getSkillValue reads per-level arrays correctly', () => {
  const player = makePlayer();
  upgradeSkill(player, 'spiritBolt'); // blocked: requires focus 1
  assert.equal(player.skills.spiritBolt, undefined);
  player.skills.focus = 1;
  upgradeSkill(player, 'spiritBolt');
  assert.equal(player.skills.spiritBolt, 1);
  assert.equal(player.hotbar[0], 'spiritBolt');
  assert.equal(getSkillValue('spiritBolt', 'mpCost', 1), 5);
});
