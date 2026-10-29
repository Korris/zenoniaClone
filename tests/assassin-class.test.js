// Assassin class data: definition, 15-skill tree (order = icon sheet rows), combo config, gear tables,
// sprite/atlas wiring, painted-icon grouping for every class, and save compatibility.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { existsSync } from 'node:fs';
import { CLASS_DEFINITIONS, CLASS_IDS, classForBranch } from '../src/data/class-definitions.js';
import { ASSASSIN_SKILL_DEFINITIONS, ASSASSIN_SKILL_ORDER } from '../src/data/skill-definitions-assassin.js';
import { SKILL_DEFINITIONS, SKILL_ORDER } from '../src/data/skill-definitions.js';
import { COMBO_CONFIG_BY_CLASS, ASSASSIN_COMBO_CONFIG, COMBO_CONFIG } from '../src/data/hero-stats-config.js';
import { OFFHAND_BY_CLASS } from '../src/data/item-definitions.js';
import { generateItem, rollEquipmentDrop } from '../src/systems/equipment-system.js';
import { createStarterItem, equipStarterGear } from '../src/systems/starter-gear.js';
import { Player } from '../src/entities/player.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { canUpgradeSkill, computePassiveModifiers } from '../src/systems/skill-system.js';
import { rollPlayerDamage } from '../src/systems/player-damage.js';
import { ASSET_FILES } from '../src/graphics/asset-atlas.js';
import { PAINTED_CLASS_IDS } from '../src/graphics/painted-combat-assets.js';
import { ASSASSIN_ART_READY, HD_ASSASSIN_ASSET_FILES } from '../src/graphics/asset-atlas-hd-assassin.js';
import { weaponIconIndex } from '../src/graphics/weapon-icons.js';

const seq = (values) => { let i = 0; return () => values[Math.min(i++, values.length - 1)]; };

test('assassin: fastest, glassiest class with extra base crit', () => {
  const { warrior, assassin } = CLASS_DEFINITIONS;
  assert.ok(CLASS_IDS.includes('assassin') && CLASS_IDS.length === 5);
  assert.equal(assassin.attackStyle, 'melee');
  assert.deepEqual(assassin.branches, ['shadow', 'venom', 'agility']);
  assert.deepEqual(assassin.baseStats, { maxHp: 88, maxMp: 50, atk: 13, def: 3, speed: 92 });
  assert.ok(assassin.baseStats.maxHp < warrior.baseStats.maxHp && assassin.baseStats.def < warrior.baseStats.def);
  for (const other of Object.values(CLASS_DEFINITIONS)) assert.ok(assassin.baseStats.speed >= other.baseStats.speed);
  const p = new Player('assassin');
  assert.equal(p.maxHp, 88);
  assert.equal(p.speed, 92);
  assert.ok(Math.abs(p.passiveMods.critChance - (0.039 + 0.06)) < 1e-9, 'class critBonus on top of the default crit');
  assert.ok(Math.abs(new Player('warrior').passiveMods.critChance - 0.039) < 1e-9);
});

test('assassin skills: 15 nodes, 3 branches x (tier1 x2, tier2 x2, tier3), icon-sheet order', () => {
  assert.deepEqual(ASSASSIN_SKILL_ORDER, [
    'shadowStep', 'backstab', 'shadowMastery', 'smokeBomb', 'phantomAssault',
    'poisonBlade', 'toxicMastery', 'venomCloud', 'crippleStrike', 'deathMark',
    'evasion', 'quickReflexes', 'bladeDance', 'adrenaline', 'shadowClone',
  ]);
  assert.equal(Object.keys(ASSASSIN_SKILL_DEFINITIONS).length, 15);
  assert.deepEqual(SKILL_ORDER.slice(60, 75), ASSASSIN_SKILL_ORDER, 'assassin = 5th group of 15 (painted icon atlas grouping)');
  const passives = ['shadowMastery', 'toxicMastery', 'evasion', 'quickReflexes'];
  ASSASSIN_SKILL_ORDER.forEach((id, i) => {
    const def = SKILL_DEFINITIONS[id];
    assert.equal(def.type, passives.includes(id) ? 'passive' : 'active', id);
    assert.equal(def.branch, ['shadow', 'venom', 'agility'][Math.floor(i / 5)], id);
    assert.equal(def.tier, [1, 1, 2, 2, 3][i % 5], id);
    assert.equal(classForBranch(def.branch), 'assassin');
    assert.equal(def.maxLevel, 5);
    for (const field of ['mpCost', 'cooldownMs', 'damageMult', 'range', 'radius', 'hasteMult', 'poisonPct', 'slowMult', 'targets', 'critBonus']) {
      if (def[field]) assert.equal(def[field].length, 5, `${id}.${field}`);
    }
    assert.ok(ASSET_FILES[def.icon], `${id} fallback icon ${def.icon}`);
    for (const req of def.requires) assert.ok(ASSASSIN_SKILL_DEFINITIONS[req.skill], `${id} requires own-tree skill`);
  });
  for (const id of ['phantomAssault', 'deathMark', 'shadowClone']) assert.equal(SKILL_DEFINITIONS[id].requires.length, 2);
});

test('painted icon groups line up with SKILL_ORDER for every class (no cross-class shift)', () => {
  assert.equal(SKILL_ORDER.length, PAINTED_CLASS_IDS.length * 15);
  PAINTED_CLASS_IDS.forEach((classId, group) => {
    for (const id of SKILL_ORDER.slice(group * 15, group * 15 + 15)) {
      assert.equal(classForBranch(SKILL_DEFINITIONS[id].branch), classId, `${id} belongs in the ${classId} icon group`);
    }
    assert.ok(ASSET_FILES[`paintedSkills${classId}`], `${classId} atlas registered`);
  });
  assert.equal(PAINTED_CLASS_IDS.indexOf('assassin'), 4);
  assert.equal(ASSET_FILES.paintedSkillsassassin, 'custom/skill-icons-assassin-roster.png');
});

test('assassin learns only its own skills; passives feed the stat modifiers', () => {
  const p = new Player('assassin');
  p.level = 30; p.skillPoints = 5;
  assert.equal(canUpgradeSkill(p, 'backstab').ok, true);
  assert.equal(canUpgradeSkill(p, 'fireball').ok, false);
  assert.equal(canUpgradeSkill(new Player('warrior'), 'backstab').reason, 'Other class');
  p.skills.shadowMastery = 5; p.skills.evasion = 3; p.skills.quickReflexes = 2; p.skills.toxicMastery = 5;
  const m = computePassiveModifiers(p);
  assert.ok(Math.abs(m.critDamage - 0.8) < 1e-9 && Math.abs(m.evasion - 0.12) < 1e-9);
  assert.ok(Math.abs(m.attackSpeed - 0.12) < 1e-9 && Math.abs(m.speedMult - 1.04) < 1e-9 && Math.abs(m.poisonMult - 1) < 1e-9);
  // Shadow Mastery's critDamage raises the crit multiplier (x1.6 -> x2.4); always-crit RNG
  const base = new Player('assassin'); base.atk = 100;
  const master = new Player('assassin'); master.atk = 100; master.passiveMods.critDamage = 0.8;
  const a = rollPlayerDamage(base, 0, 1, () => 0).damage, b = rollPlayerDamage(master, 0, 1, () => 0).damage;
  assert.equal(Math.round(b / a * 100), 150);
});

test('assassin combo: four quick dagger hits, finisher is a double stab, shorter reach than the warrior', () => {
  assert.equal(COMBO_CONFIG_BY_CLASS.assassin, ASSASSIN_COMBO_CONFIG);
  assert.equal(new Player('assassin').combo.config, ASSASSIN_COMBO_CONFIG);
  const { hits } = ASSASSIN_COMBO_CONFIG;
  assert.equal(hits.length, 4);
  assert.equal(hits[3].stabs, 2);
  hits.slice(0, 3).forEach((h) => assert.ok(!h.stabs));
  hits.forEach((h, i) => {
    assert.ok(h.reach < COMBO_CONFIG.hits[i].reach, `hit ${i + 1} shorter reach`);
    assert.ok(h.windupMs + h.activeMs + h.recoverMs < COMBO_CONFIG.hits[i].windupMs + COMBO_CONFIG.hits[i].activeMs + COMBO_CONFIG.hits[i].recoverMs, `hit ${i + 1} quicker`);
  });
});

test('assassin gear: drops for everyone, Dagger naming, dagger offhand, starter set, warrior blade icons', () => {
  assert.deepEqual(OFFHAND_BY_CLASS.assassin, { type: 'dagger', name: 'Off-hand Dagger' });
  const weapon = generateItem(seq([0.3]), 1, 'weapon', 'common', 'assassin');
  assert.equal(weapon.classId, 'assassin');
  assert.equal(weapon.name, 'Dagger');
  // drop class index = floor(rand * 5): assassin is the last index
  const drop = rollEquipmentDrop({ itemLevel: 5 }, seq([0, 0.95, 5.5 / 6, 0, 0.5, 0.5]));
  assert.equal(drop.classId, 'assassin');
  const p = new Player('assassin');
  assert.equal(equipStarterGear(p), true);
  assert.equal(p.equipment.weapon.name, 'Dagger');
  assert.equal(p.equipment.weapon.classId, 'assassin');
  assert.equal(p.equipment.offhand.templateId, 'dagger');
  assert.equal(createStarterItem('offhand', 'assassin').classId, 'assassin');
  assert.equal(weaponIconIndex(5, 'assassin'), weaponIconIndex(5, 'warrior'));
});

test('assassin save round-trip keeps class, skills, hotbar; foreign skills dropped', () => {
  const p = new Player('assassin');
  p.skills.backstab = 3; p.skills.evasion = 2; p.skills.fireball = 4;
  p.hotbar[0] = 'backstab';
  equipStarterGear(p);
  const loaded = validateSave(JSON.parse(JSON.stringify(serializeSave({ player: p, mapId: 'oakhavenVillage', x: 1, y: 1 }))));
  assert.equal(loaded.player.classId, 'assassin');
  assert.deepEqual(loaded.player.skills, { backstab: 3, evasion: 2 });
  assert.equal(loaded.player.hotbar[0], 'backstab');
  assert.equal(loaded.player.equipment.weapon.classId, 'assassin');
  const q = new Player('assassin');
  q.applySaveData(loaded.player);
  assert.equal(q.skills.backstab, 3);
  assert.equal(validateSave({ ...loaded, player: { ...loaded.player, classId: 'nope' } }).player.classId, 'warrior');
});

test('assassin art wiring: real sheets under hd/assassin, loader key present', () => {
  assert.equal(ASSASSIN_ART_READY, true);
  assert.ok(Object.keys(HD_ASSASSIN_ASSET_FILES).length >= 9);
  for (const file of Object.values(HD_ASSASSIN_ASSET_FILES)) {
    assert.ok(existsSync(new URL(`../assets/${file}`, import.meta.url)), `${file} exists (no 404s)`);
    assert.ok(file.startsWith('hd/assassin/'), 'real assassin art');
  }
  assert.ok(Object.values(ASSET_FILES).includes(HD_ASSASSIN_ASSET_FILES.hdAssassinIdle));
});
