// Playable classes: per-class stats and combo, class-locked skill trees, saves keep the class, and
// the wizard's ranged combo (bolts + exploding finisher).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { CLASS_DEFINITIONS, CLASS_IDS, DEFAULT_CLASS_ID } from '../src/data/class-definitions.js';
import { statsForLevel } from '../src/systems/leveling-system.js';
import { Player } from '../src/entities/player.js';
import { Camera } from '../src/core/camera.js';
import { WorldState } from '../src/game/world-state.js';
import { Monster } from '../src/entities/monster.js';
import { serializeSave, validateSave, SAVE_VERSION } from '../src/save/save-serializer.js';
import { canUpgradeSkill } from '../src/systems/skill-system.js';
import { fireComboBolt, resolveProjectileHits } from '../src/systems/projectile-hit-system.js';
import { WIZARD_COMBO_CONFIG } from '../src/data/hero-stats-config.js';

test('warrior, wizard, mechanic, cleric and assassin exist; wizard trades HP/DEF for MP', () => {
  assert.deepEqual([...CLASS_IDS].sort(), ['assassin', 'cleric', 'mechanic', 'warrior', 'wizard']);
  const w = statsForLevel(10, 'warrior');
  const m = statsForLevel(10, 'wizard');
  assert.ok(m.maxMp > w.maxMp && m.maxHp < w.maxHp && m.def < w.def);
  assert.deepEqual(statsForLevel(5), statsForLevel(5, DEFAULT_CLASS_ID)); // default class = warrior
});

test('Player takes its class: stats, attack style and combo config', () => {
  const wizard = new Player('wizard');
  assert.equal(wizard.classId, 'wizard');
  assert.equal(wizard.attackStyle, 'ranged');
  assert.equal(wizard.maxMp, CLASS_DEFINITIONS.wizard.baseStats.maxMp);
  assert.equal(wizard.combo.config, WIZARD_COMBO_CONFIG);
  assert.equal(new Player().classId, 'warrior');
  assert.equal(new Player('nonsense').classId, 'warrior');
});

test('each class can only learn skills of its own tree', () => {
  const wizard = new Player('wizard');
  wizard.level = 30;
  wizard.skillPoints = 5;
  assert.equal(canUpgradeSkill(wizard, 'fireball').ok, true);
  assert.equal(canUpgradeSkill(wizard, 'slashWave').ok, false);
  const warrior = new Player('warrior');
  warrior.level = 30;
  warrior.skillPoints = 5;
  assert.equal(canUpgradeSkill(warrior, 'slashWave').ok, true);
  assert.equal(canUpgradeSkill(warrior, 'fireball').ok, false);
});

test('saves keep the class; old saves load as warrior; other-class skills are dropped', () => {
  const wizard = new Player('wizard');
  wizard.skills.fireball = 2;
  const loaded = validateSave(JSON.parse(JSON.stringify(serializeSave({ player: wizard, mapId: 'oakhavenVillage', x: 1, y: 1 }))));
  assert.equal(loaded.player.classId, 'wizard');
  assert.equal(loaded.player.skills.fireball, 2);

  const legacy = validateSave({ version: SAVE_VERSION, player: { level: 3, exp: 0, skillPoints: 0, skills: { slashWave: 1 } }, mapId: 'oakhavenVillage' });
  assert.equal(legacy.player.classId, 'warrior');
  assert.equal(legacy.player.skills.slashWave, 1);

  const tampered = validateSave({ version: SAVE_VERSION, player: { classId: 'wizard', level: 3, exp: 0, skillPoints: 0, skills: { slashWave: 3, iceShard: 1 } }, mapId: 'oakhavenVillage' });
  assert.deepEqual(tampered.player.skills, { iceShard: 1 });
});

function wizardWorld() {
  const player = new Player('wizard');
  player.x = 100;
  player.y = 100;
  player.dir = 'right';
  return new WorldState(player, new Camera());
}

test('wizard combo steps fire bolts in the facing direction; the finisher explodes', () => {
  const world = wizardWorld();
  const { combo } = world.player;
  combo.startHit(1);
  fireComboBolt(world, 1);
  const bolt = world.projectiles[0];
  assert.ok(bolt.vx > 0 && bolt.vy === 0 && bolt.owner === 'player');
  assert.equal(bolt.explodeRadius, 0);
  combo.startHit(4);
  fireComboBolt(world, 4);
  assert.ok(world.projectiles[1].explodeRadius > 0);
});

test('an exploding bolt damages every monster in its radius once', () => {
  const world = wizardWorld();
  world.hitRoll = () => 0.5; // Test blast coverage independently of ordinary-attack MISS rolls.
  world.player.combo.startHit(4);
  fireComboBolt(world, 4);
  const bolt = world.projectiles[0];
  const near = new Monster('slime', bolt.x + 2, bolt.y + 6);
  const nearby = new Monster('slime', bolt.x + 14, bolt.y + 10);
  const far = new Monster('slime', bolt.x + 120, bolt.y + 6);
  world.monsters.push(near, nearby, far);
  resolveProjectileHits(world);
  assert.ok(bolt.dead);
  assert.ok(near.hp < near.maxHp && nearby.hp < nearby.maxHp, 'both monsters in the blast are hit');
  assert.equal(far.hp, far.maxHp, 'monster outside the blast is untouched');
});

test('every active skill of every class casts without throwing (real world + monster nearby)', async () => {
  const { SKILL_ORDER, SKILL_DEFINITIONS } = await import('../src/data/skill-definitions.js');
  const { tryCastSkill } = await import('../src/systems/skill-cast-system.js');
  const { classForBranch } = await import('../src/data/class-definitions.js');
  for (const id of SKILL_ORDER.filter((s) => SKILL_DEFINITIONS[s].type === 'active')) {
    const player = new Player(classForBranch(SKILL_DEFINITIONS[id].branch));
    player.x = 100;
    player.y = 100;
    player.dir = 'right';
    player.skills[id] = 1;
    player.mp = 999;
    const world = new WorldState(player, new Camera());
    world.monsters.push(new Monster('slime', 112, 100));
    const result = tryCastSkill(world, id);
    assert.ok(result.ok, `${id}: ${result.reason}`);
  }
});
