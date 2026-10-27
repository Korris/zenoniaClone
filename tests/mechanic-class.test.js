// Mechanic class: stats sit between warrior and wizard, the gun combo fires fast bullets with a
// muzzle flash and an exploding plasma finisher, and every class's HD art (sheets, side rows,
// 4-key-pose walk cycles) is wired to files that exist with the sliced grid size.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { CLASS_DEFINITIONS } from '../src/data/class-definitions.js';
import { MECHANIC_COMBO_CONFIG, WIZARD_COMBO_CONFIG } from '../src/data/hero-stats-config.js';
import { MECHANIC_SKILL_DEFINITIONS } from '../src/data/skill-definitions-mechanic.js';
import { ASSET_FILES, HD_HERO_ANIMATIONS } from '../src/graphics/asset-atlas.js';
import { HD_WIZARD_ANIMATIONS } from '../src/graphics/asset-atlas-hd-wizard.js';
import { HD_MECHANIC_ANIMATIONS, HD_MECHANIC_SIDE_ROWS, HD_MECHANIC_FX, HD_MECHANIC_FACE_KEY } from '../src/graphics/asset-atlas-hd-mechanic.js';
import { Player } from '../src/entities/player.js';
import { Camera } from '../src/core/camera.js';
import { WorldState } from '../src/game/world-state.js';
import { fireComboBolt } from '../src/systems/projectile-hit-system.js';

const pngSize = (path) => { const buf = readFileSync(path); return { width: buf.readUInt32BE(16), height: buf.readUInt32BE(20) }; };

test('mechanic: ranged gunner between warrior and wizard', () => {
  const { warrior, wizard, mechanic } = CLASS_DEFINITIONS;
  assert.equal(mechanic.attackStyle, 'ranged');
  assert.ok(mechanic.baseStats.maxHp < warrior.baseStats.maxHp && mechanic.baseStats.maxHp > wizard.baseStats.maxHp);
  assert.ok(mechanic.baseStats.maxMp > warrior.baseStats.maxMp && mechanic.baseStats.maxMp < wizard.baseStats.maxMp);
  const p = new Player('mechanic');
  assert.equal(p.combo.config, MECHANIC_COMBO_CONFIG);
  assert.notEqual(p.combo.config, WIZARD_COMBO_CONFIG);
  for (const def of Object.values(MECHANIC_SKILL_DEFINITIONS)) assert.ok(mechanic.branches.includes(def.branch), def.id);
});

test('mechanic gun combo: bullets out-range and out-speed wizard bolts; finisher explodes', () => {
  const player = new Player('mechanic');
  Object.assign(player, { x: 100, y: 100, dir: 'left' });
  const world = new WorldState(player, new Camera());
  player.combo.startHit(1);
  fireComboBolt(world, 1);
  const bullet = world.projectiles[0];
  assert.ok(bullet.vx < 0 && bullet.vy === 0 && bullet.spriteKey === 'bullet' && bullet.explodeRadius === 0);
  assert.ok(MECHANIC_COMBO_CONFIG.hits[0].bolt.speed > WIZARD_COMBO_CONFIG.hits[0].bolt.speed);
  assert.equal(world.effects.at(-1).spriteKey, 'muzzleFlash');
  player.combo.startHit(4);
  fireComboBolt(world, 4);
  assert.equal(world.projectiles[1].explodeFx, 'explosion');
  assert.ok(world.projectiles[1].explodeRadius > 0);
});

test('every HD class sheet, portrait and gun FX file exists with the sliced size', () => {
  const walkCols = {};
  for (const [cls, anims] of [['hero', HD_HERO_ANIMATIONS], ['wizard', HD_WIZARD_ANIMATIONS], ['mechanic', HD_MECHANIC_ANIMATIONS]]) {
    for (const [anim, key] of Object.entries(anims)) {
      const path = 'assets/' + ASSET_FILES[key];
      assert.ok(existsSync(path), `${cls}/${anim}: ${path} missing`);
      const { width, height } = pngSize(path);
      assert.equal(height, 4 * 96, `${path}: 4 facing rows of 96px`);
      assert.equal(width % 96, 0, `${path}: whole 96px columns`);
      if (anim === 'walk') walkCols[cls] = width / 96;
    }
  }
  // 4-key-pose walk (left step / pass / right step / pass) for every class -- the old 6-frame walks
  // read as same-arm-same-leg shuffles.
  assert.deepEqual(walkCols, { hero: 4, wizard: 4, mechanic: 4 });
  assert.ok(existsSync('assets/' + ASSET_FILES[HD_MECHANIC_FACE_KEY]));
  for (const key of Object.values(HD_MECHANIC_FX)) assert.ok(existsSync('assets/' + ASSET_FILES[key]), key);
});

test('mechanic side rows are row 2 left / row 3 right, or a mirrored right row', () => {
  for (const anim of Object.keys(HD_MECHANIC_ANIMATIONS)) {
    const side = HD_MECHANIC_SIDE_ROWS[anim];
    assert.equal(side.right, 3);
    assert.ok(side.left === 2 || side.left === 'mirror', anim);
  }
});
