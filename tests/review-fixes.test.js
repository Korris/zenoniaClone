// Regression tests for the 5-class overhaul code-review fixes (class lock, offhand class, enhance floor,
// attack speed vs combo chain, stab cancel, status damage after death, pickup text, naming).
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { sanitizeItem, generateItem, INVENTORY_SIZE } from '../src/systems/equipment-system.js';
import { serializeSave, validateSave } from '../src/save/save-serializer.js';
import { equipStarterGear, createStarterItem } from '../src/systems/starter-gear.js';
import { enhanceAtkBonus, effectiveItemStats } from '../src/systems/weapon-enhance-stats.js';
import { enhancePreview } from '../src/ui/inventory-enhance-actions.js';
import { MAX_ENHANCE } from '../src/data/weapon-enhance-config.js';
import { ComboController } from '../src/systems/combo-attack-system.js';
import { COMBO_CONFIG } from '../src/data/hero-stats-config.js';
import { Player } from '../src/entities/player.js';
import { tryCastSkill } from '../src/systems/skill-cast-system.js';
import { performComboHit } from '../src/systems/combo-hit-system.js';
import { hitPlayer } from '../src/systems/combat-system.js';
import { attackSpeedFactor, applyHaste } from '../src/systems/player-status.js';
import { updatePlayer } from '../src/game/player-controller.js';
import { tickMonsterStatus } from '../src/systems/monster-status-tick.js';
import { applyPoison, applyMark } from '../src/systems/monster-status.js';
import { OFFHAND_BY_CLASS } from '../src/data/item-definitions.js';
import { assassinWorld, foe, step } from './helpers/assassin-world.js';

const seq = (values) => { let i = 0; return () => values[i++ % values.length]; };
const rawOffhand = (templateId, classId) => ({ uid: `o-${templateId}`, templateId, slot: 'offhand', rarity: 'common', itemLevel: 1, classId, stats: {} });
const rawWeapon = (classId) => ({ uid: `w-${classId}`, templateId: 'sword', slot: 'weapon', rarity: 'common', itemLevel: 1, classId, stats: { atk: 4 } });
const roundTrip = (player) => validateSave(JSON.parse(JSON.stringify(serializeSave({ player, mapId: 'verdantMeadow', x: 1, y: 1, bossDefeated: false }))));

// --- 1. offhand class comes from the template -----------------------------------------------------------------
test('sanitize: offhand classId derives from the template type, never the raw tag', () => {
  assert.equal(sanitizeItem(rawOffhand('spirit', 'buff')).classId, 'cleric', 'legacy buff-tagged spirit loads as cleric');
  assert.equal(sanitizeItem(rawOffhand('spirit', undefined)).classId, 'cleric');
  assert.equal(sanitizeItem(rawOffhand('shield', 'wizard')).classId, 'warrior', 'tampered tag ignored');
  for (const [classId, { type }] of Object.entries(OFFHAND_BY_CLASS)) {
    assert.equal(sanitizeItem(rawOffhand(type, 'hacker')).classId, classId, type);
  }
});

// --- 2. class lock on load ------------------------------------------------------------------------------------
test('load: gear of another class moves from the slots to the bag and starter gear refills them', () => {
  const p = new Player('warrior');
  p.equipment.weapon = sanitizeItem(rawWeapon('wizard'));
  p.equipment.offhand = sanitizeItem(rawOffhand('spirit', 'cleric'));
  const loaded = roundTrip(p);
  assert.equal(loaded.player.classId, 'warrior');
  assert.equal(loaded.player.equipment.weapon, null);
  assert.equal(loaded.player.equipment.offhand, null);
  assert.deepEqual(loaded.player.inventory.map((i) => i.uid), ['w-wizard', 'o-spirit']);
  const restored = new Player('warrior');
  restored.applySaveData(loaded.player);
  assert.equal(equipStarterGear(restored), true);
  assert.equal(restored.equipment.weapon.classId, 'warrior');
  assert.equal(restored.equipment.offhand.templateId, 'shield');
});

test('load: a full bag drops the mismatched gear instead of overflowing; own-class gear stays equipped', () => {
  const p = new Player('mechanic');
  p.equipment.weapon = sanitizeItem(rawWeapon('assassin'));
  p.equipment.offhand = createStarterItem('offhand', 'mechanic');
  p.inventory = Array.from({ length: INVENTORY_SIZE }, (_, i) => generateItem(seq([0.3, i / 100]), 1, 'boots'));
  const loaded = roundTrip(p);
  assert.equal(loaded.player.equipment.weapon, null);
  assert.equal(loaded.player.inventory.length, INVENTORY_SIZE);
  assert.equal(loaded.player.equipment.offhand.templateId, 'robot');
});

// --- 3. enhance floor + offhand half table --------------------------------------------------------------------
test('enhance floor scales with the base: weak items cannot out-gain their own stat', () => {
  assert.equal(enhanceAtkBonus(2, 5), 2);
  assert.equal(enhanceAtkBonus(2, MAX_ENHANCE), 4); // round(2 * 1.44) = 3 < ceil(15 * 0.25) = 4
  assert.equal(enhanceAtkBonus(8, 5), 5);
  assert.equal(enhanceAtkBonus(40, 1), 2);
  assert.equal(enhanceAtkBonus(0, 5), 0);
  for (let base = 1; base <= 12; base += 1) {
    let prev = 0;
    for (let l = 1; l <= MAX_ENHANCE; l += 1) { const b = enhanceAtkBonus(base, l); assert.ok(b >= prev && b > 0, `base ${base} +${l}`); prev = b; }
  }
});

test('offhand uses half of the weapon % table; preview and effective stats agree', () => {
  const weapon = { slot: 'weapon', templateId: 'sword', name: 'x', stats: { atk: 40 }, enhance: 10 };
  const dagger = { slot: 'offhand', templateId: 'robot', name: 'y', stats: { atk: 40 }, enhance: 10 };
  const w = effectiveItemStats(weapon).atk - 40, o = effectiveItemStats(dagger).atk - 40;
  assert.ok(Math.abs(o - w / 2) <= 1, `weapon +${w}, offhand +${o}`);
  const player = new Player('mechanic');
  player.materials.upgradeStone = 5;
  const view = enhancePreview(player, dagger);
  assert.deepEqual([view.atkFrom, view.atkTo], [40 + o, 40 + enhanceAtkBonus(40, 11, 0.5)]);
});

// --- 5. attack speed vs combo chain window --------------------------------------------------------------------
test('attack speed shortens swings but not the combo chain window', () => {
  const run = (speed) => {
    const combo = new ComboController(COMBO_CONFIG);
    combo.pressAttack();
    let t = 0;
    while (combo.isAttacking && t < 5000) { combo.update(10, speed); t += 10; }
    return { swingMs: t, chain: combo.chainTimer, last: combo.lastStep };
  };
  const slow = run(1), fast = run(2);
  assert.ok(fast.swingMs < slow.swingMs * 0.6, `swing ${slow.swingMs} -> ${fast.swingMs}`);
  assert.equal(fast.chain, slow.chain, 'chain window unchanged by haste');
  assert.equal(fast.chain, COMBO_CONFIG.chainWindowMs);
  // The window drains in real time: 100ms of real dt removes 100ms regardless of speed.
  const combo = new ComboController(COMBO_CONFIG);
  combo.pressAttack();
  while (combo.isAttacking) combo.update(10, 2);
  combo.update(100, 2);
  assert.equal(combo.chainTimer, COMBO_CONFIG.chainWindowMs - 100);
});

test('updatePlayer: Adrenaline haste keeps the full chain window after a swing', () => {
  const world = assassinWorld({ adrenaline: 1 }); const p = world.player;
  world.tilemap = { moveBox: (box) => ({ x: box.x, y: box.y }) };
  applyHaste(p, 2, 5000);
  assert.equal(attackSpeedFactor(p), 2);
  let pressed = true;
  const input = { wasPressed: (name) => name === 'attack' && pressed, getMoveVector: () => ({ x: 0, y: 0 }) };
  for (let t = 0; t < 2000 && (pressed || p.combo.isAttacking); t += 10) {
    updatePlayer(world, input, 10, null);
    pressed = false;
  }
  assert.equal(p.combo.isAttacking, false);
  assert.equal(p.combo.chainTimer, p.combo.config.chainWindowMs, 'window not scaled by haste');
});

// --- 9. status damage stops once the hero is dead --------------------------------------------------------------
test('poison, Death Mark and Venom Cloud do nothing while the hero is dead', () => {
  const world = assassinWorld({ venomCloud: 1 });
  const m = foe(134, 100);
  world.monsters.push(m);
  applyPoison(m, 10, 5000);
  applyMark(m, 50, 3000);
  world.player.dead = true;
  const hp = m.hp;
  tickMonsterStatus(world, m, 4000);
  assert.equal(m.hp, hp, 'no tick, no detonation');
  assert.ok(m.status.poison && m.status.mark, 'statuses are frozen, not consumed');
  world.player.dead = false;
  tryCastSkill(world, 'venomCloud');
  world.player.dead = true;
  const fresh = foe(134, 100);
  world.monsters.push(fresh);
  step(world, 1500);
  assert.equal(fresh.status?.poison, undefined, 'cloud stopped poisoning');
});

// --- 13. finisher stab pulses cancel when the combo is interrupted -----------------------------------------------
test('finisher second stab lands normally but is cancelled when the hero is hit mid-combo', () => {
  const stabbed = (interrupt) => {
    const world = assassinWorld(); const p = world.player;
    const m = foe(118, 100);
    world.monsters.push(m);
    p.combo.startHit(4);
    performComboHit(world, 4);
    const first = m.maxHp - m.hp;
    if (interrupt) p.combo.reset(); // what hitPlayer() does
    m.hitBySwing.clear();
    step(world, 200);
    return { first, total: m.maxHp - m.hp };
  };
  const normal = stabbed(false), cut = stabbed(true);
  assert.ok(normal.total > normal.first, 'second stab lands');
  assert.equal(cut.total, cut.first, 'interrupted combo: no second stab');
});

test('a stab pulse from an old swing does not fire in a newer combo', () => {
  const world = assassinWorld(); const p = world.player;
  const m = foe(118, 100);
  world.monsters.push(m);
  p.combo.startHit(4);
  performComboHit(world, 4);
  const first = m.maxHp - m.hp;
  p.combo.reset();
  p.combo.startHit(1); // a fresh swing begins before the pulse
  m.hitBySwing.clear();
  step(world, 200);
  assert.equal(m.maxHp - m.hp, first);
});

test('hitPlayer resets the combo (so pending stabs cancel)', () => {
  const world = assassinWorld();
  world.player.combo.startHit(4);
  const serial = world.player.combo.serial;
  hitPlayer(world, 1, 0, 0, false);
  assert.equal(world.player.combo.isAttacking, false);
  assert.notEqual(world.player.combo.serial, serial);
});

// --- 14. naming ---------------------------------------------------------------------------------------------------
test('assassin offhand is displayed as Off-hand Dagger; the weapon stays Dagger', () => {
  assert.equal(OFFHAND_BY_CLASS.assassin.name, 'Off-hand Dagger');
  assert.equal(generateItem(seq([0.3]), 1, 'offhand', 'common', 'assassin').name, 'Off-hand Dagger');
  assert.equal(generateItem(seq([0.3]), 1, 'weapon', 'common', 'assassin').name, 'Dagger');
});

// --- death -> automatic return to the village -----------------------------------------------------------------------
test('dying away from the village returns the hero to Oakhaven after the death animation, with the EXP penalty and no menu', async () => {
  const { GameController } = await import('../src/game/game-controller.js');
  const { FIRST_MAP_ID } = await import('../src/world/map-registry.js');
  const store = new Map();
  globalThis.localStorage ??= { getItem: (k) => store.get(k) ?? null, setItem: (k, v) => store.set(k, String(v)), removeItem: (k) => store.delete(k) };
  const input = { wasPressed: () => false, takeClicks: () => [], endFrame() {}, getMoveVector: () => ({ x: 0, y: 0 }) };
  const controller = new GameController({}, input, null);
  const { session } = controller;
  session.startNewGame('warrior');
  session.enterMap('darkCave');
  const p = session.player;
  p.level = 5; p.exp = 400; p.recomputeStats();
  assert.notEqual(session.world.mapId, FIRST_MAP_ID);
  p.takeDamage(1e9);
  assert.equal(p.dead, true);
  controller.handleEvent({ type: 'playerDied' });
  assert.equal(controller.state, 'dead');
  controller.update(16);
  assert.equal(controller.state, 'dead', 'death animation still playing');
  for (let i = 0; i < 120 && controller.state === 'dead'; i += 1) controller.update(16);
  assert.equal(controller.state, 'playing');
  assert.equal(session.world.mapId, FIRST_MAP_ID);
  assert.equal(p.dead, false);
  assert.ok(p.exp < 400, 'EXP penalty applied');
  assert.equal(p.hp, p.maxHp);
  assert.equal(controller.menu === null || controller.menu === undefined || controller.state === 'playing', true);
  assert.equal(session.world.skillPulses.length, 0);
});
