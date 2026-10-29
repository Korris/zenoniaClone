import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {}, fillRect() {} }; }
};
const { WARRIOR_HELD_ANCHORS } = await import('../src/data/warrior-held-anchors-generated.js');
const { WARRIOR_SWORD_OVERLAYS } = await import('../src/data/warrior-sword-overlay-manifest-generated.js');
const { WARRIOR_SHIELD_OVERLAYS } = await import('../src/data/warrior-shield-overlay-manifest-generated.js');
const { buildHeldGear } = await import('../src/graphics/held-weapon-registry.js');
const { resolveHeldGear, drawHeldHero } = await import('../src/graphics/held-weapon-renderer.js');
const { HELD_WEAPON_ASSET_FILES, WARRIOR_BARE_ANIMATIONS, warriorSwordImageKey, warriorShieldImageKey } = await import('../src/graphics/asset-atlas-held-weapon.js');

const COUNTS = { idle: 4, walk: 4, attack1: 4, attack2: 4, attack3: 6, dash: 3, hurt: 2 };
const DIRS = ['down', 'up', 'left', 'right'];
const item = (slot, itemLevel, enhance = 0) => ({ slot, itemLevel, enhance });

test('manifests: 10 tiers, unique names, grips inside art, glow colour', () => {
  for (const manifest of [WARRIOR_SWORD_OVERLAYS, WARRIOR_SHIELD_OVERLAYS]) {
    assert.equal(manifest.length, 10);
    assert.equal(new Set(manifest.map((e) => e.name)).size, 10);
    for (const e of manifest) {
      assert.match(e.glow, /^#[0-9a-f]{6}$/);
      for (const v of ['base', 'enhanced']) {
        assert.ok(e[v].gripX >= 0 && e[v].gripX < e[v].w && e[v].gripY >= 0 && e[v].gripY < e[v].h, `${e.name} ${v} grip`);
      }
    }
  }
});

test('anchors cover every anim x dir x frame for sword AND shield', () => {
  for (const kind of ['sword', 'shield']) {
    for (const [anim, n] of Object.entries(COUNTS)) {
      for (const dir of DIRS) {
        const list = WARRIOR_HELD_ANCHORS[kind][anim][dir];
        assert.equal(list.length, n, `${kind}.${anim}.${dir} count`);
        list.forEach((a, i) => {
          assert.ok(!a.hidden, `${kind}.${anim}.${dir}.${i} unfilled`);
          assert.ok(a.x > -20 && a.x < 212 && a.y > -20 && a.y < 212, `${kind}.${anim}.${dir}.${i} grip in frame`);
          assert.ok(Number.isFinite(a.angle) && a.s > 0.1 && a.s < 0.6 && a.sx > 0.3 && a.sx <= 1, `${kind}.${anim}.${dir}.${i} pose`);
        });
      }
    }
  }
});

test('asset files exist on disk for every warrior sheet and overlay', () => {
  for (const [key, path] of Object.entries(HELD_WEAPON_ASSET_FILES)) {
    if (/Wizard/i.test(key)) continue;
    assert.ok(fs.existsSync(new URL(`../assets/${path}`, import.meta.url)), path);
  }
  assert.ok(fs.existsSync(new URL('../assets/hd/warrior-v2/normalized/hero_face.png', import.meta.url)));
});

const make = () => Object.fromEntries(DIRS.map((d) => [d, Object.fromEntries(Object.entries(COUNTS).map(([a, n]) => [a, Array.from({ length: n }, () => ({}))]))]));
const images = Object.fromEntries([
  ...WARRIOR_SWORD_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [warriorSwordImageKey(t, v), { width: e[v].w, height: e[v].h }])),
  ...WARRIOR_SHIELD_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [warriorShieldImageKey(t, v), { width: e[v].w, height: e[v].h }])),
]);
function setup(sets = {}) {
  const warrior = make(), bare = make(), partial = make();
  const slicer = (_img, anims) => (anims === WARRIOR_BARE_ANIMATIONS ? bare : partial);
  return { warrior, bare, partial, gear: buildHeldGear(sets.images ?? images, 'warrior', warrior, slicer, {}) };
}
const registry = (gear) => ({ heldGear: { warrior: gear } });
const player = (equipment) => ({ classId: 'warrior', equipment });

test('registry pairs baked frame with bare + partial body and both anchors; death stays baked', () => {
  const { warrior, bare, partial, gear } = setup();
  const e = gear.byFrame.get(warrior.left.attack3[2]);
  assert.equal(e.bare, bare.left.attack3[2]);
  assert.equal(e.partial, partial.left.attack3[2]);
  assert.equal(e.anchors.sword, WARRIOR_HELD_ANCHORS.sword.attack3.left[2]);
  assert.equal(e.anchors.shield, WARRIOR_HELD_ANCHORS.shield.attack3.left[2]);
  assert.equal(setup({ images: {} }).gear, null, 'no sword art => null (baked sheet)');
});

test('tier + band selection for weapon and offhand independently; shield only with an offhand', () => {
  const { warrior, bare, gear } = setup();
  const frame = warrior.down.idle[0];
  const held = resolveHeldGear(registry(gear), player({ weapon: item('weapon', 45, 0), offhand: item('offhand', 95, 15) }), frame);
  assert.equal(held.body, bare.down.idle[0]);
  assert.deepEqual(held.layers.map((l) => l.id), ['shield', 'sword']);
  assert.equal(held.layers[0].baked.body !== images[warriorShieldImageKey(9, 'enhanced')], true, 'band 4 => glow-baked canvas');
  assert.equal(held.layers[0].band, 4);
  assert.ok(held.layers[0].baked.aura, 'band 4 aura');
  assert.equal(held.layers[1].band, 0);
  assert.equal(held.layers[1].baked.body, images[warriorSwordImageKey(4, 'base')]);
  const noShield = resolveHeldGear(registry(gear), player({ weapon: item('weapon', 5), offhand: null }), frame);
  assert.deepEqual(noShield.layers.map((l) => l.id), ['sword'], 'empty offhand => bare fist, no shield');
  assert.equal(noShield.body, bare.down.idle[0]);
  const noWeapon = resolveHeldGear(registry(gear), player({}), frame);
  assert.equal(noWeapon.layers[0].id === 'sword' || noWeapon.layers.length === 0, true);
});

test('fallbacks: unknown frame, other class, missing shield anchors keep the shield painted in the body', () => {
  const { warrior, partial, gear } = setup();
  assert.equal(resolveHeldGear(registry(gear), player({}), { other: 1 }), null);
  assert.equal(resolveHeldGear(registry(gear), { classId: 'mechanic', equipment: {} }, warrior.down.idle[0]), null);
  assert.equal(resolveHeldGear({}, player({}), warrior.down.idle[0]), null);
  const entry = gear.byFrame.get(warrior.down.hurt[0]);
  delete entry.anchors.shield; // simulate a missing shield anchor for this anim
  const held = resolveHeldGear(registry(gear), player({ weapon: item('weapon', 5), offhand: item('offhand', 5) }), warrior.down.hurt[0]);
  assert.equal(held.body, partial.down.hurt[0]);
  assert.deepEqual(held.layers.map((l) => l.id), ['sword']);
});

test('draw order: behind layers, body, front layers; hidden skipped', () => {
  const calls = [];
  const ctx = { save() {}, restore() {}, translate() {}, rotate() {}, scale() {}, drawImage: () => calls.push('gear') };
  const body = () => calls.push('body');
  const baked = { body: { width: 10, height: 10 }, aura: null, gripX: 5, gripY: 5 };
  const layer = (anchor) => ({ anchor, baked });
  const pose = { x: 1, y: 1, angle: 0, s: 0.3, sx: 0.8 };
  drawHeldHero(ctx, { body: {}, layers: [layer({ ...pose, behind: true }), layer({ ...pose, behind: false })] }, body, 0, 0, 24, 0);
  assert.deepEqual(calls.splice(0), ['gear', 'body', 'gear']);
  drawHeldHero(ctx, { body: {}, layers: [layer({ hidden: true })] }, body, 0, 0, 24, 0);
  assert.deepEqual(calls.splice(0), ['body']);
});
