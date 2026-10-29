import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {}, fillRect() {} }; }
};
const { CLERIC_ART_READY, HD_CLERIC_ASSET_FILES, HD_CLERIC_SIDE_ROWS, HD_CLERIC_ANIMATIONS } = await import('../src/graphics/asset-atlas-hd-cleric.js');
const { CLERIC_STAFF_ANCHORS } = await import('../src/data/cleric-held-anchors-generated.js');
const { CLERIC_STAFF_OVERLAYS } = await import('../src/data/cleric-staff-overlay-manifest-generated.js');
const { CLERIC_SPIRIT_OVERLAYS } = await import('../src/data/cleric-spirit-overlay-manifest-generated.js');
const { CLERIC_SPIRIT_FLOAT } = await import('../src/data/cleric-spirit-float-manifest-generated.js');
const { HELD_WEAPON_ASSET_FILES, CLERIC_BARE_ANIMATIONS, clericStaffImageKey, clericSpiritImageKey, clericSpiritFloatKey } = await import('../src/graphics/asset-atlas-held-weapon.js');
const { buildHeldGear } = await import('../src/graphics/held-weapon-registry.js');
const { resolveHeldGear } = await import('../src/graphics/held-weapon-renderer.js');
const { COMPANION_OFFSETS, companionTarget, createCompanionState, stepCompanion, companionBob, companionVisible } = await import('../src/entities/companion-follow.js');
const { buildCompanion, companionVariant, resolveCompanionArt } = await import('../src/graphics/companion-renderer.js');
const { COMPANION_SPECS } = await import('../src/graphics/companion-specs.js');
const { CLERIC_COMBO_CONFIG } = await import('../src/data/hero-stats-config.js');
const { HOLY_FX_TINT } = await import('../src/data/cleric-fx-config.js');
const { Projectile } = await import('../src/entities/projectile.js');

const COUNTS = { idle: 4, walk: 4, attack1: 4, attack2: 4, attack3: 6, dash: 3, hurt: 2 };
const DIRS = ['down', 'up', 'left', 'right'];
const item = (slot, itemLevel, enhance = 0) => ({ slot, itemLevel, enhance });
const exists = (path) => fs.existsSync(new URL(`../assets/${path}`, import.meta.url));

test('flag on, armed sheets + portrait exist, both side rows painted (no mirroring)', () => {
  assert.equal(CLERIC_ART_READY, true);
  assert.equal(Object.keys(HD_CLERIC_ASSET_FILES).length, Object.keys(HD_CLERIC_ANIMATIONS).length + 1);
  for (const path of Object.values(HD_CLERIC_ASSET_FILES)) assert.ok(exists(path), path);
  for (const side of Object.values(HD_CLERIC_SIDE_ROWS)) assert.deepEqual(side, { right: 3, left: 2 });
});

test('manifests: 10 tiers each, grips inside art, float loop 4 facings x 4 frames, all files on disk', () => {
  for (const manifest of [CLERIC_STAFF_OVERLAYS, CLERIC_SPIRIT_OVERLAYS]) {
    assert.equal(manifest.length, 10);
    assert.equal(new Set(manifest.map((e) => e.name)).size, 10);
    for (const e of manifest) {
      assert.match(e.glow, /^#[0-9a-f]{6}$/);
      for (const v of ['base', 'enhanced']) assert.ok(e[v].gripX >= 0 && e[v].gripX < e[v].w && e[v].gripY >= 0 && e[v].gripY < e[v].h, `${e.name} ${v}`);
    }
  }
  assert.deepEqual(Object.keys(CLERIC_SPIRIT_FLOAT), DIRS);
  for (const frames of Object.values(CLERIC_SPIRIT_FLOAT)) assert.equal(frames.length, 4);
  for (const [key, path] of Object.entries(HELD_WEAPON_ASSET_FILES)) if (/Cleric/.test(key)) assert.ok(exists(path), path);
  assert.ok(exists('hd/cleric/normalized/hero_face.png'));
});

test('staff anchors cover every anim x dir x frame; each is a finite pose or an explicit fallback; few fallbacks', () => {
  let fallbacks = 0, total = 0;
  for (const [anim, n] of Object.entries(COUNTS)) {
    for (const dir of DIRS) {
      const list = CLERIC_STAFF_ANCHORS[anim][dir];
      assert.equal(list.length, n, `${anim}.${dir} count`);
      list.forEach((a, i) => {
        total++;
        if (a.fallback) { fallbacks++; return; }
        assert.ok(Number.isFinite(a.x) && Number.isFinite(a.y) && Number.isFinite(a.angle), `${anim}.${dir}.${i} pose`);
        assert.ok(a.s > 0.2 && a.s < 0.8 && a.sx > 0.3 && a.sx <= 1, `${anim}.${dir}.${i} scale`);
      });
    }
  }
  assert.ok(fallbacks / total < 0.2, `${fallbacks}/${total} frames fall back to the baked armed frame`);
  assert.equal(CLERIC_STAFF_ANCHORS.death, undefined, 'death has no staff overlay (armed death frames are drawn)');
});

const make = () => Object.fromEntries(DIRS.map((d) => [d, Object.fromEntries(Object.entries(COUNTS).map(([a, n]) => [a, Array.from({ length: n }, () => ({}))]))]));
const images = Object.fromEntries(CLERIC_STAFF_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [clericStaffImageKey(t, v), { width: e[v].w, height: e[v].h }])));

test('registry: per-frame fallback draws the baked frame; others compose bare body + staff at tier/band', () => {
  const cleric = make(), bare = make();
  const gear = buildHeldGear(images, 'cleric', cleric, (_i, anims) => (anims === CLERIC_BARE_ANIMATIONS ? bare : null), {});
  const reg = { heldGear: { cleric: gear } };
  const player = (weapon) => ({ classId: 'cleric', equipment: { weapon } });
  const ok = (() => { for (const [anim, n] of Object.entries(COUNTS)) for (let i = 0; i < n; i++) if (!CLERIC_STAFF_ANCHORS[anim].down[i].fallback) return [anim, i]; })();
  const held = resolveHeldGear(reg, player(item('weapon', 45, 15)), cleric.down[ok[0]][ok[1]]);
  assert.equal(held.body, bare.down[ok[0]][ok[1]]);
  assert.equal(held.layers.length, 1);
  assert.equal(held.layers[0].band, 4);
  assert.equal(gear.byFrame.get(cleric.down.hurt[0]) !== undefined, true);
  // force a fallback on one frame
  const frame = cleric.right.walk[1];
  gear.byFrame.get(frame).anchors.staff = { fallback: true };
  assert.equal(resolveHeldGear(reg, player(item('weapon', 5)), frame), null, 'fallback => baked armed frame');
  assert.equal(gear.byFrame.get(cleric.down.death?.[0] ?? {}), undefined, 'death not paired');
  assert.equal(buildHeldGear({}, 'cleric', cleric, () => bare), null, 'no staff art => baked');
});

test('spirit follow: target by facing, eases toward it, snaps after a teleport, bobs', () => {
  const player = { x: 100, y: 100, dir: 'down', classId: 'cleric', equipment: { offhand: item('offhand', 5) } };
  const state = createCompanionState(player);
  assert.deepEqual({ x: state.x, y: state.y }, companionTarget(player));
  player.x = 140; // hero walks away
  stepCompanion(state, player, 16);
  assert.ok(state.x > 111 && state.x < 151, 'moved part of the way');
  for (let i = 0; i < 60; i++) stepCompanion(state, player, 16);
  assert.ok(Math.abs(state.x - (140 + COMPANION_OFFSETS.down.x)) < 0.5, 'converged');
  player.dir = 'left';
  assert.equal(companionTarget(player).x, 140 + COMPANION_OFFSETS.left.x);
  player.x = 900; // teleport / map change
  stepCompanion(state, player, 16);
  assert.deepEqual({ x: state.x, y: state.y }, companionTarget(player));
  assert.ok(Math.abs(companionBob(375)) > Math.abs(companionBob(0)) && Math.abs(companionBob(375)) <= 1.3);
});

test('spirit visible only for a living cleric with an offhand', () => {
  const base = { classId: 'cleric', dead: false, equipment: { offhand: item('offhand', 5) } };
  assert.equal(companionVisible(base), true);
  assert.equal(companionVisible({ ...base, equipment: { offhand: null } }), false);
  assert.equal(companionVisible({ ...base, equipment: {} }), false);
  assert.equal(companionVisible({ ...base, dead: true }), false);
  assert.equal(companionVisible({ ...base, classId: 'wizard' }), false);
});

const spiritImages = Object.fromEntries([
  ...CLERIC_SPIRIT_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [clericSpiritImageKey(t, v), { width: e[v].w, height: e[v].h }])),
  ...Object.entries(CLERIC_SPIRIT_FLOAT).flatMap(([dir, fs_]) => fs_.map((f, i) => [clericSpiritFloatKey(dir, i), { width: f.w, height: f.h }])),
]);

test('spirit art: tier from item level, enhanced + glow band from +7, float loop only for plain tier 1', () => {
  assert.deepEqual(companionVariant(item('offhand', 95, 15)), { tier: 9, band: 4, enhanced: true });
  assert.deepEqual(companionVariant(item('offhand', 5, 6)), { tier: 0, band: 0, enhanced: false });
  assert.equal(companionVariant(item('offhand', 45, 7)).band, 1);
  assert.equal(buildCompanion({}, COMPANION_SPECS.cleric), null);
  const spirit = buildCompanion(spiritImages, COMPANION_SPECS.cleric);
  const a0 = resolveCompanionArt(spirit, item('offhand', 5, 0), 'left', 0), a1 = resolveCompanionArt(spirit, item('offhand', 5, 0), 'left', 150);
  assert.equal(a0.animated, true);
  assert.notEqual(a0.baked.body, a1.baked.body, 'float frames advance');
  const tier4 = resolveCompanionArt(spirit, item('offhand', 45, 0), 'left', 0);
  assert.equal(tier4.animated, false);
  assert.equal(tier4.baked.body, spiritImages[clericSpiritImageKey(4, 'base')]);
  const glow = resolveCompanionArt(spirit, item('offhand', 95, 15), 'down', 0);
  assert.equal(glow.band, 4);
  assert.ok(glow.baked.aura, 'band 4 aura');
  assert.equal(resolveCompanionArt(null, item('offhand', 5), 'down', 0), null);
});

test('cleric combo orbs carry the holy tint into Projectile', () => {
  for (const hit of CLERIC_COMBO_CONFIG.hits) assert.equal(hit.bolt.tint, HOLY_FX_TINT);
  const p = new Projectile({ x: 0, y: 0, vx: 1, vy: 0, owner: 'player', damage: 1, spriteKey: 'magicBolt', lifeMs: 100, w: 4, h: 4, tint: HOLY_FX_TINT });
  assert.equal(p.tint, HOLY_FX_TINT);
  assert.equal(new Projectile({ x: 0, y: 0, vx: 1, vy: 0, owner: 'player', damage: 1, spriteKey: 'x', lifeMs: 1, w: 1, h: 1 }).tint, null);
});
