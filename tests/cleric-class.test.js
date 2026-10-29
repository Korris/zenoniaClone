// Cleric class data: definition, 15-skill tree (order = icon sheet rows), combo config, gear tables,
// sprite/atlas wiring and save compatibility.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { CLASS_DEFINITIONS, CLASS_IDS, classForBranch } from '../src/data/class-definitions.js';
import { CLERIC_SKILL_DEFINITIONS, CLERIC_SKILL_ORDER } from '../src/data/skill-definitions-cleric.js';
import { SKILL_DEFINITIONS, SKILL_ORDER } from '../src/data/skill-definitions.js';
import { COMBO_CONFIG_BY_CLASS, CLERIC_COMBO_CONFIG, WIZARD_COMBO_CONFIG } from '../src/data/hero-stats-config.js';
import { OFFHAND_BY_CLASS } from '../src/data/item-definitions.js';
import { generateItem, rollEquipmentDrop } from '../src/systems/equipment-system.js';
import { createStarterItem, equipStarterGear } from '../src/systems/starter-gear.js';
import { Player } from '../src/entities/player.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { canUpgradeSkill, computePassiveModifiers } from '../src/systems/skill-system.js';
import { ASSET_FILES } from '../src/graphics/asset-atlas.js';
import { PAINTED_CLASS_IDS } from '../src/graphics/painted-combat-assets.js';
import { CLERIC_ART_READY, HD_CLERIC_ASSET_FILES } from '../src/graphics/asset-atlas-hd-cleric.js';
import { weaponIconIndex } from '../src/graphics/weapon-icons.js';

const seq = (values) => { let i = 0; return () => values[Math.min(i++, values.length - 1)]; };

test('cleric: MP-rich ranged support between wizard and mechanic in durability', () => {
  const { wizard, mechanic, cleric } = CLASS_DEFINITIONS;
  assert.ok(CLASS_IDS.includes('cleric'));
  assert.equal(cleric.attackStyle, 'ranged');
  assert.deepEqual(cleric.branches, ['holy', 'restoration', 'blessing']);
  assert.ok(cleric.baseStats.maxHp > wizard.baseStats.maxHp && cleric.baseStats.maxHp < mechanic.baseStats.maxHp);
  assert.ok(cleric.baseStats.maxMp >= wizard.baseStats.maxMp);
  assert.deepEqual(cleric.baseStats, { maxHp: 85, maxMp: 75, atk: 11, def: 4, speed: 80 });
  const p = new Player('cleric');
  assert.equal(p.classId, 'cleric');
  assert.equal(p.maxHp, 85);
  assert.equal(p.combo.config, CLERIC_COMBO_CONFIG);
  assert.notEqual(p.combo.config, WIZARD_COMBO_CONFIG);
});

test('cleric skills: 15 nodes, 3 branches x (tier1 x2, tier2 x2, tier3), icon-sheet order', () => {
  assert.equal(CLERIC_SKILL_ORDER.length, 15);
  assert.deepEqual(CLERIC_SKILL_ORDER, [
    'holyBolt', 'smite', 'divineJudgment', 'consecrate', 'heavenlyRay',
    'heal', 'regeneration', 'healingCircle', 'purify', 'resurrectionGrace',
    'blessingOfMight', 'sacredShield', 'hasteHymn', 'spiritBond', 'angelsDescent',
  ]);
  assert.equal(Object.keys(CLERIC_SKILL_DEFINITIONS).length, 15);
  assert.deepEqual(SKILL_ORDER.slice(45, 60), CLERIC_SKILL_ORDER, 'cleric = 4th group of 15 (painted icon atlas grouping)');
  const types = {
    holyBolt: 'active', smite: 'active', divineJudgment: 'active', consecrate: 'passive', heavenlyRay: 'active',
    heal: 'active', regeneration: 'passive', healingCircle: 'active', purify: 'active', resurrectionGrace: 'passive',
    blessingOfMight: 'active', sacredShield: 'active', hasteHymn: 'active', spiritBond: 'passive', angelsDescent: 'active',
  };
  CLERIC_SKILL_ORDER.forEach((id, i) => {
    const def = SKILL_DEFINITIONS[id];
    assert.equal(def.type, types[id], id);
    assert.equal(def.branch, ['holy', 'restoration', 'blessing'][Math.floor(i / 5)], id);
    assert.equal(def.tier, [1, 1, 2, 2, 3][i % 5], id);
    assert.equal(classForBranch(def.branch), 'cleric');
    assert.equal(def.maxLevel, 5);
    for (const field of ['mpCost', 'cooldownMs', 'damageMult', 'healPercent', 'shieldPercent', 'hasteMult']) {
      if (def[field]) assert.equal(def[field].length, 5, `${id}.${field}`);
    }
    assert.ok(ASSET_FILES[def.icon], `${id} fallback icon ${def.icon}`);
    for (const req of def.requires) assert.ok(CLERIC_SKILL_DEFINITIONS[req.skill], `${id} requires own-tree skill`);
  });
  // tier-3 nodes need two prerequisites each
  for (const id of ['heavenlyRay', 'resurrectionGrace', 'angelsDescent']) assert.equal(SKILL_DEFINITIONS[id].requires.length, 2);
});

test('cleric can only learn cleric skills; passives feed computePassiveModifiers', () => {
  const p = new Player('cleric');
  p.level = 30; p.skillPoints = 5;
  assert.equal(canUpgradeSkill(p, 'heal').ok, true);
  assert.equal(canUpgradeSkill(p, 'fireball').ok, false);
  assert.equal(canUpgradeSkill(new Player('wizard'), 'heal').reason, 'Other class');
  p.skills.consecrate = 5; p.skills.resurrectionGrace = 2; p.skills.spiritBond = 1;
  const mods = computePassiveModifiers(p);
  assert.ok(mods.comboMult > 0.29 && mods.surviveHpPct === 30 && mods.graceCooldownMs === 110000 && mods.hpMult > 1.03);
});

test('cleric combo: four steps (three orbs + bursting finisher), same finisher rule as wizard', () => {
  assert.equal(COMBO_CONFIG_BY_CLASS.cleric, CLERIC_COMBO_CONFIG);
  const { hits } = CLERIC_COMBO_CONFIG;
  assert.equal(hits.length, 4);
  hits.slice(0, 3).forEach((h) => assert.ok(!h.bolt.explodeRadius));
  assert.ok(hits[3].bolt.explodeRadius > 0 && hits[3].mult > hits[2].mult);
});

test('cleric gear: drop list, Holy Staff naming, spirit offhand, starter set', () => {
  assert.equal(OFFHAND_BY_CLASS.buff, undefined);
  assert.deepEqual(OFFHAND_BY_CLASS.cleric, { type: 'spirit', name: 'Spirit' });
  const weapon = generateItem(seq([0.3]), 1, 'weapon', 'common', 'cleric');
  assert.equal(weapon.classId, 'cleric');
  assert.equal(weapon.name, 'Holy Staff');
  // drop class index = floor(rand * 5): cleric is index 3, and it is tradeable gear
  const drop = rollEquipmentDrop({ itemLevel: 5 }, seq([0, 3.5 / 5, 5.5 / 6, 0, 0.5, 0.5]));
  assert.equal(drop.classId, 'cleric');
  const p = new Player('cleric');
  assert.equal(equipStarterGear(p), true);
  assert.equal(p.equipment.weapon.name, 'Holy Staff');
  assert.equal(p.equipment.weapon.classId, 'cleric');
  assert.equal(p.equipment.offhand.templateId, 'spirit');
  assert.equal(createStarterItem('offhand', 'cleric').classId, 'cleric');
  assert.equal(weaponIconIndex(5, 'cleric'), weaponIconIndex(5, 'wizard'), 'staff icons reused until cleric art exists');
});

test('cleric save round-trip keeps class, skills, hotbar; old saves unaffected', () => {
  const p = new Player('cleric');
  p.skills.heal = 3; p.skills.holyBolt = 2; p.skills.fireball = 4; // fireball belongs to the wizard -> dropped
  p.hotbar[0] = 'heal';
  equipStarterGear(p);
  const loaded = validateSave(JSON.parse(JSON.stringify(serializeSave({ player: p, mapId: 'oakhavenVillage', x: 1, y: 1 }))));
  assert.equal(loaded.player.classId, 'cleric');
  assert.deepEqual(loaded.player.skills, { heal: 3, holyBolt: 2 });
  assert.equal(loaded.player.hotbar[0], 'heal');
  assert.equal(loaded.player.equipment.weapon.classId, 'cleric');
  const q = new Player('cleric');
  q.applySaveData(loaded.player);
  assert.equal(q.skills.heal, 3);
  const old = validateSave({ version: loaded.version, player: { level: 3, exp: 0, skillPoints: 0, skills: { slashWave: 1 } }, mapId: 'oakhavenVillage' });
  assert.equal(old.player.classId, 'warrior');
});

test('cleric art wiring: icon atlas registered, sheets only requested once art is ready', () => {
  assert.ok(PAINTED_CLASS_IDS.indexOf('cleric') === 3);
  assert.equal(ASSET_FILES.paintedSkillscleric, 'custom/skill-icons-cleric-roster.png');
  assert.equal(Object.keys(HD_CLERIC_ASSET_FILES).length > 0, CLERIC_ART_READY);
});
