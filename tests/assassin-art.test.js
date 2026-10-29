import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {}, fillRect() {} }; }
};
const { ASSASSIN_HELD_ANCHORS } = await import('../src/data/assassin-held-anchors-generated.js');
const { ASSASSIN_DAGGER_OVERLAYS } = await import('../src/data/assassin-dagger-overlay-manifest-generated.js');
const { ASSASSIN_ART_READY, HD_ASSASSIN_ASSET_FILES, HD_ASSASSIN_SIDE_ROWS } = await import('../src/graphics/asset-atlas-hd-assassin.js');
const { HELD_WEAPON_ASSET_FILES, ASSASSIN_BARE_ANIMATIONS, assassinDaggerImageKey } = await import('../src/graphics/asset-atlas-held-weapon.js');
const { buildHeldGear } = await import('../src/graphics/held-weapon-registry.js');
const { resolveHeldGear } = await import('../src/graphics/held-weapon-renderer.js');
const { HELD_WEAPON_CLASSES } = await import('../src/graphics/held-weapon-config.js');
const { gearIconRef } = await import('../src/graphics/gear-icons.js');

const COUNTS = { idle: 4, walk: 4, attack1: 4, attack2: 4, attack3: 6, dash: 3, hurt: 2 };
const DIRS = ['down', 'up', 'left', 'right'];
const HANDS = ['main', 'off'];
const item = (slot, itemLevel, enhance = 0) => ({ slot, itemLevel, enhance });
const exists = (path) => fs.existsSync(new URL(`../assets/${path}`, import.meta.url));

test('assassin art is live: all sheets + portrait + weaponless sheets + 20 dagger overlays exist', () => {
  assert.equal(ASSASSIN_ART_READY, true);
  for (const path of Object.values(HD_ASSASSIN_ASSET_FILES)) assert.ok(exists(path), path);
  for (const [key, path] of Object.entries(HELD_WEAPON_ASSET_FILES)) if (/Assassin/.test(key)) assert.ok(exists(path), path);
  assert.deepEqual(Object.keys(ASSASSIN_BARE_ANIMATIONS).sort(), Object.keys(COUNTS).sort());
  for (const rows of Object.values(HD_ASSASSIN_SIDE_ROWS)) assert.deepEqual(rows, { right: 3, left: 2 }, 'both side rows are painted');
});

test('dagger manifest: 10 tiers, grips inside art, glow colour', () => {
  assert.equal(ASSASSIN_DAGGER_OVERLAYS.length, 10);
  for (const e of ASSASSIN_DAGGER_OVERLAYS) {
    assert.match(e.glow, /^#[0-9a-f]{6}$/);
    for (const v of ['base', 'enhanced']) assert.ok(e[v].gripX >= 0 && e[v].gripX < e[v].w && e[v].gripY >= 0 && e[v].gripY < e[v].h, `${e.name} ${v}`);
  }
});

test('anchors: both hands x every anim x dir x frame are finite poses or whole-frame fallbacks', () => {
  let fallbacks = 0, total = 0;
  for (const [anim, n] of Object.entries(COUNTS)) {
    for (const dir of DIRS) {
      const [main, off] = HANDS.map((h) => ASSASSIN_HELD_ANCHORS[h][anim][dir]);
      assert.equal(main.length, n, `${anim}.${dir} main count`);
      assert.equal(off.length, n, `${anim}.${dir} off count`);
      main.forEach((a, i) => {
        total++;
        if (a.fallback) { fallbacks++; return; }
        for (const b of [a, off[i]]) {
          assert.ok(!b.fallback, `${anim}.${dir}.${i}: a frame is fallback on both hands or neither`);
          assert.ok(Number.isFinite(b.x) && Number.isFinite(b.y) && Number.isFinite(b.angle), `${anim}.${dir}.${i} pose`);
          assert.ok(b.s > 0.1 && b.s < 0.4 && b.sx > 0.3 && b.sx <= 1, `${anim}.${dir}.${i} scale`);
        }
      });
    }
  }
  assert.ok(fallbacks / total < 0.4, `${fallbacks}/${total} frames fall back`);
});

const make = () => Object.fromEntries(DIRS.map((d) => [d, Object.fromEntries(Object.entries(COUNTS).map(([a, n]) => [a, Array.from({ length: n }, () => ({}))]))]));
const images = Object.fromEntries(ASSASSIN_DAGGER_OVERLAYS.flatMap((e, t) => ['base', 'enhanced'].map((v) => [assassinDaggerImageKey(t, v), { width: e[v].w, height: e[v].h }])));

test('registry: two daggers per frame, each by its own slot tier / band; empty offhand => one dagger; fallback => baked', () => {
  const assassin = make(), bare = make();
  const gear = buildHeldGear(images, 'assassin', assassin, (_i, anims) => (anims === ASSASSIN_BARE_ANIMATIONS ? bare : null), {});
  assert.ok(HELD_WEAPON_CLASSES.includes('assassin'));
  const reg = { heldGear: { assassin: gear } };
  const player = (weapon, offhand) => ({ classId: 'assassin', equipment: { weapon, offhand } });
  const ok = (() => { for (const [anim, n] of Object.entries(COUNTS)) for (let i = 0; i < n; i++) if (!ASSASSIN_HELD_ANCHORS.main[anim].down[i].fallback) return [anim, i]; })();
  const frame = assassin.down[ok[0]][ok[1]];
  const both = resolveHeldGear(reg, player(item('weapon', 95, 15), item('offhand', 5)), frame);
  assert.equal(both.body, bare.down[ok[0]][ok[1]]);
  assert.deepEqual(both.layers.map((l) => l.id).sort(), ['main', 'off']);
  assert.equal(both.layers.find((l) => l.id === 'main').band, 4, 'main uses the weapon slot enhance band');
  assert.equal(both.layers.find((l) => l.id === 'off').band, 0, 'off uses the offhand slot enhance band');
  assert.deepEqual(resolveHeldGear(reg, player(item('weapon', 5), null), frame).layers.map((l) => l.id), ['main']);
  const walk = assassin.down.walk[0], held = gear.byFrame.get(walk);
  held.anchors.off = { fallback: true };
  assert.equal(resolveHeldGear(reg, player(item('weapon', 5), item('offhand', 5)), walk), null, 'whole-frame fallback');
  assert.equal(buildHeldGear({}, 'assassin', assassin, () => bare), null, 'no dagger art => baked');
});

test('dagger icons resolve for the assassin weapon and the Dagger offhand only', () => {
  assert.deepEqual(gearIconRef({ slot: 'weapon', itemLevel: 95, classId: 'assassin' }, 'warrior'), { classId: 'assassin', slot: 'weapon', tier: 9 });
  assert.deepEqual(gearIconRef({ slot: 'offhand', itemLevel: 5, templateId: 'dagger' }, 'assassin'), { classId: 'assassin', slot: 'offhand', tier: 0 });
  assert.equal(gearIconRef({ slot: 'weapon', itemLevel: 5, classId: 'warrior' }, 'warrior').classId, 'warrior');
  assert.equal(gearIconRef({ slot: 'offhand', itemLevel: 5, templateId: 'shield' }, 'assassin').classId, 'warrior', 'shield art belongs to the warrior');
  assert.equal(gearIconRef({ slot: 'weapon', itemLevel: 5, classId: 'wizard' }, 'assassin'), null, 'no spec for wizard');
});
