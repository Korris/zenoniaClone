import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {}, fillRect() {} }; }
};
const { MECHANIC_GUN_ANCHORS } = await import('../src/data/mechanic-held-anchors-generated.js');
const { MECHANIC_GUN_OVERLAYS } = await import('../src/data/mechanic-gun-overlay-manifest-generated.js');
const { MECHANIC_ROBOT_OVERLAYS } = await import('../src/data/mechanic-robot-overlay-manifest-generated.js');
const { MECHANIC_ROBOT_FLOAT } = await import('../src/data/mechanic-robot-float-manifest-generated.js');
const { HELD_WEAPON_ASSET_FILES, MECHANIC_BARE_ANIMATIONS, mechanicGunImageKey, mechanicRobotImageKey, mechanicRobotFloatKey } =
  await import('../src/graphics/asset-atlas-held-weapon.js');
const { buildHeldGear } = await import('../src/graphics/held-weapon-registry.js');
const { resolveHeldGear } = await import('../src/graphics/held-weapon-renderer.js');
const { HELD_WEAPON_CLASSES } = await import('../src/graphics/held-weapon-config.js');
const { companionVisible } = await import('../src/entities/companion-follow.js');
const { buildCompanion, buildCompanions, resolveCompanionArt } = await import('../src/graphics/companion-renderer.js');
const { COMPANION_SPECS } = await import('../src/graphics/companion-specs.js');

const COUNTS = { idle: 4, walk: 4, attack1: 4, attack3: 6, dash: 3, hurt: 2 };
const DIRS = ['down', 'up', 'left', 'right'];
const item = (slot, itemLevel, enhance = 0) => ({ slot, itemLevel, enhance });
const exists = (path) => fs.existsSync(new URL(`../assets/${path}`, import.meta.url));

test('mechanic is a held-gear class; weaponless sheets cover everything but attack2 / death', () => {
  assert.ok(HELD_WEAPON_CLASSES.includes('mechanic'));
  assert.deepEqual(Object.keys(MECHANIC_BARE_ANIMATIONS).sort(), Object.keys(COUNTS).sort());
  for (const [key, path] of Object.entries(HELD_WEAPON_ASSET_FILES)) if (/Mechanic/.test(key)) assert.ok(exists(path), path);
});

test('gun / robot manifests: 10 tiers, grips inside art; float loop 4 facings x 4 frames', () => {
  for (const manifest of [MECHANIC_GUN_OVERLAYS, MECHANIC_ROBOT_OVERLAYS]) {
    assert.equal(manifest.length, 10);
    for (const e of manifest) {
      assert.match(e.glow, /^#[0-9a-f]{6}$/);
      for (const v of ['base', 'enhanced']) assert.ok(e[v].gripX >= 0 && e[v].gripX < e[v].w && e[v].gripY >= 0 && e[v].gripY < e[v].h, `${e.name} ${v}`);
    }
  }
  assert.deepEqual(Object.keys(MECHANIC_ROBOT_FLOAT), DIRS);
  for (const frames of Object.values(MECHANIC_ROBOT_FLOAT)) assert.equal(frames.length, 4);
});

test('gun anchors: every anim x dir x frame is a finite pose (negative sx = mirrored) or a fallback; attack2 / death absent', () => {
  let fallbacks = 0, total = 0, mirrored = 0;
  for (const [anim, n] of Object.entries(COUNTS)) {
    for (const dir of DIRS) {
      const list = MECHANIC_GUN_ANCHORS[anim][dir];
      assert.equal(list.length, n, `${anim}.${dir} count`);
      list.forEach((a, i) => {
        total++;
        if (a.fallback) { fallbacks++; return; }
        assert.ok(Number.isFinite(a.x) && Number.isFinite(a.y) && Number.isFinite(a.angle), `${anim}.${dir}.${i} pose`);
        assert.ok(a.s > 0.08 && a.s < 0.5 && Math.abs(a.sx) > 0.3 && Math.abs(a.sx) <= 1, `${anim}.${dir}.${i} scale`);
        if (a.sx < 0) mirrored++;
      });
    }
  }
  assert.ok(fallbacks / total < 0.4, `${fallbacks}/${total} frames fall back to the baked armed frame`);
  assert.ok(mirrored > 0, 'left-facing guns are mirrored');
  assert.equal(MECHANIC_GUN_ANCHORS.attack2, undefined);
  assert.equal(MECHANIC_GUN_ANCHORS.death, undefined);
});

const make = () => Object.fromEntries(DIRS.map((d) => [d, Object.fromEntries([...Object.entries(COUNTS), ['attack2', 4]].map(([a, n]) => [a, Array.from({ length: n }, () => ({}))]))]));
const gunImages = Object.fromEntries(MECHANIC_GUN_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [mechanicGunImageKey(t, v), { width: e[v].w, height: e[v].h }])));

test('registry: attack2 stays baked; fallback frames draw baked; others compose bare body + gun at tier/band', () => {
  const mechanic = make(), bare = make();
  const gear = buildHeldGear(gunImages, 'mechanic', mechanic, (_i, anims) => (anims === MECHANIC_BARE_ANIMATIONS ? bare : null), {});
  const reg = { heldGear: { mechanic: gear } };
  const player = (weapon) => ({ classId: 'mechanic', equipment: { weapon } });
  assert.equal(gear.byFrame.get(mechanic.down.attack2[0]), undefined, 'attack2 is not paired: baked frames');
  assert.equal(resolveHeldGear(reg, player(item('weapon', 5)), mechanic.down.attack2[0]), null);
  const ok = (() => { for (const [anim, n] of Object.entries(COUNTS)) for (let i = 0; i < n; i++) if (!MECHANIC_GUN_ANCHORS[anim].down[i].fallback) return [anim, i]; })();
  const held = resolveHeldGear(reg, player(item('weapon', 95, 15)), mechanic.down[ok[0]][ok[1]]);
  assert.equal(held.body, bare.down[ok[0]][ok[1]]);
  assert.equal(held.layers[0].band, 4);
  gear.byFrame.get(mechanic.right.walk[1]).anchors.gun = { fallback: true };
  assert.equal(resolveHeldGear(reg, player(item('weapon', 5)), mechanic.right.walk[1]), null);
  assert.equal(buildHeldGear({}, 'mechanic', mechanic, () => bare), null, 'no gun art => baked');
});

const robotImages = Object.fromEntries([
  ...MECHANIC_ROBOT_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [mechanicRobotImageKey(t, v), { width: e[v].w, height: e[v].h }])),
  ...Object.entries(MECHANIC_ROBOT_FLOAT).flatMap(([dir, fs_]) => fs_.map((f, i) => [mechanicRobotFloatKey(dir, i), { width: f.w, height: f.h }])),
]);

test('robot companion: shared follower shows it for a living mechanic with an offhand; tier art + float loop', () => {
  const base = { classId: 'mechanic', dead: false, equipment: { offhand: item('offhand', 5) } };
  assert.equal(companionVisible(base), true);
  assert.equal(companionVisible({ ...base, equipment: { offhand: null } }), false);
  assert.equal(companionVisible({ ...base, dead: true }), false);
  assert.equal(companionVisible({ ...base, classId: 'warrior' }), false);
  assert.equal(buildCompanion({}, COMPANION_SPECS.mechanic), null);
  assert.deepEqual(Object.keys(buildCompanions(robotImages)), ['mechanic']);
  const robot = buildCompanion(robotImages, COMPANION_SPECS.mechanic);
  const a0 = resolveCompanionArt(robot, item('offhand', 5, 0), 'up', 0), a1 = resolveCompanionArt(robot, item('offhand', 5, 0), 'up', 150);
  assert.equal(a0.animated, true);
  assert.notEqual(a0.baked.body, a1.baked.body);
  const tier4 = resolveCompanionArt(robot, item('offhand', 45, 0), 'up', 0);
  assert.equal(tier4.baked.body, robotImages[mechanicRobotImageKey(4, 'base')]);
  assert.equal(resolveCompanionArt(robot, item('offhand', 95, 15), 'down', 0).band, 4);
});
