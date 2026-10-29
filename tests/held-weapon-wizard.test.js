import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';

globalThis.OffscreenCanvas ??= class {
  constructor(w, h) { this.width = w; this.height = h; }
  getContext() { return { drawImage() {}, fillRect() {} }; }
};
const { WIZARD_STAFF_ANCHORS } = await import('../src/data/wizard-staff-anchors-generated.js');
const { WIZARD_STAFF_OVERLAYS } = await import('../src/data/wizard-staff-overlay-manifest-generated.js');
const { heldTierOf, heldBandOf } = await import('../src/graphics/held-weapon-config.js');
const { buildHeldGear } = await import('../src/graphics/held-weapon-registry.js');
const { resolveHeldGear, drawHeldHero } = await import('../src/graphics/held-weapon-renderer.js');
const { bakeStaffGlow, GLOW_PAD } = await import('../src/graphics/held-weapon-glow-baker.js');
const { HELD_BARE_ANIMATIONS, HELD_WEAPON_ASSET_FILES, staffImageKey } = await import('../src/graphics/asset-atlas-held-weapon.js');

const COUNTS = { idle: 4, walk: 4, attack1: 4, attack2: 4, attack3: 6, dash: 3, hurt: 2, death: 5 };
const DIRS = ['down', 'up', 'left', 'right'];
const weapon = (itemLevel, enhance = 0) => ({ slot: 'weapon', itemLevel, enhance });

test('enhance band mapping 0..15 -> 0/1/2/3/4', () => {
  const expected = [0, 0, 0, 0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 4];
  expected.forEach((band, level) => assert.equal(heldBandOf(weapon(5, level)), band, `+${level}`));
  assert.equal(heldBandOf(null), 0);
});

test('tier follows equipmentTier(itemLevel); no weapon => apprentice (0)', () => {
  assert.equal(heldTierOf(null), 0);
  assert.equal(heldTierOf(weapon(5)), 0);
  assert.equal(heldTierOf(weapon(55)), 5);
  assert.equal(heldTierOf(weapon(99)), 9);
});

test('anchors cover every anim x dir x frame with sane values', () => {
  for (const [anim, n] of Object.entries(COUNTS)) {
    for (const dir of DIRS) {
      const list = WIZARD_STAFF_ANCHORS[anim]?.[dir];
      assert.equal(list?.length, n, `${anim}.${dir} count`);
      list.forEach((a, i) => {
        if (a.hidden) return;
        const tag = `${anim}.${dir}.${i}`;
        assert.ok(a.x >= -20 && a.x <= 212 && a.y >= -20 && a.y <= 212, `${tag} grip in frame`);
        assert.ok(Number.isFinite(a.angle) && a.s > 0.15 && a.s < 0.7, `${tag} angle/scale`);
      });
    }
  }
  assert.ok(WIZARD_STAFF_ANCHORS.death.down.every((a) => a.hidden), 'staff hidden during death');
});

test('asset files exist on disk for every weaponless sheet and overlay', () => {
  for (const path of Object.values(HELD_WEAPON_ASSET_FILES)) {
    assert.ok(fs.existsSync(new URL(`../assets/${path}`, import.meta.url)), path);
  }
});

function fakeSet(images) {
  const make = () => Object.fromEntries(DIRS.map((d) => [d, Object.fromEntries(Object.entries(COUNTS).map(([a, n]) => [a, Array.from({ length: n }, () => ({ w: 192 }))]))]));
  const wizard = make(), bare = make();
  return { images, wizard, buildHeroHd: () => bare, bare };
}
const overlayImages = Object.fromEntries(WIZARD_STAFF_OVERLAYS.flatMap((e, t) => ['base', 'enhanced']
  .map((v) => [staffImageKey(t, v), { width: e[v].w, height: e[v].h }])));

test('registry pairs baked frames with bare frames and anchors; missing art falls back', () => {
  const { wizard, buildHeroHd, bare } = fakeSet();
  const staff = buildHeldGear(overlayImages, 'wizard', wizard, buildHeroHd, {});
  const frame = wizard.left.attack3[2];
  assert.equal(staff.byFrame.get(frame).bare, bare.left.attack3[2]);
  assert.equal(staff.byFrame.get(frame).anchors.staff, WIZARD_STAFF_ANCHORS.attack3.left[2]);
  assert.equal(buildHeldGear({}, 'wizard', wizard, buildHeroHd, {}), null, 'no overlay art => null');
  const partial = fakeSet();
  delete partial.bare.up.dash; // one direction missing for an animation
  const s2 = buildHeldGear(overlayImages, 'wizard', partial.wizard, partial.buildHeroHd, {});
  assert.equal(s2.byFrame.get(partial.wizard.down.dash[0]), undefined, 'dash falls back to baked');
  assert.ok(s2.byFrame.get(partial.wizard.down.idle[0]));
});

test('resolveHeldStaff: wizard only, tier + band selection, unknown frame => null', () => {
  const { wizard, buildHeroHd } = fakeSet();
  const heldStaff = buildHeldGear(overlayImages, 'wizard', wizard, buildHeroHd, {});
  const registry = { heldGear: { wizard: heldStaff } };
  const frame = wizard.down.idle[0];
  assert.equal(resolveHeldGear(registry, { classId: 'warrior', equipment: {} }, frame), null);
  assert.equal(resolveHeldGear(registry, { classId: 'wizard', equipment: {} }, { other: 1 }), null);
  assert.equal(resolveHeldGear({}, { classId: 'wizard', equipment: {} }, frame), null);
  const none = resolveHeldGear(registry, { classId: 'wizard', equipment: { weapon: null } }, frame);
  assert.equal(none.layers[0].baked.body, overlayImages[staffImageKey(0, 'base')]);
  const base = resolveHeldGear(registry, { classId: 'wizard', equipment: { weapon: weapon(55, 6) } }, frame);
  assert.equal(base.layers[0].baked.body, overlayImages[staffImageKey(5, 'base')]);
  const enh = resolveHeldGear(registry, { classId: 'wizard', equipment: { weapon: weapon(95, 15) } }, frame);
  assert.equal(enh.layers[0].band, 4);
  assert.ok(enh.layers[0].baked.aura, 'band 4 has an aura layer');
});

test('glow baker: band 0 untouched; bands 1-4 padded and cached', () => {
  const art = { img: { width: 40, height: 100 }, gripX: 20, gripY: 55 };
  assert.equal(bakeStaffGlow(art, '#5972ff', 0).body, art.img);
  const b1 = bakeStaffGlow(art, '#5972ff', 1);
  assert.deepEqual([b1.body.width, b1.body.height, b1.gripX, b1.gripY], [40 + 2 * GLOW_PAD, 100 + 2 * GLOW_PAD, 20 + GLOW_PAD, 55 + GLOW_PAD]);
  assert.equal(bakeStaffGlow(art, '#5972ff', 1), b1);
  assert.equal(bakeStaffGlow(art, '#5972ff', 3).aura, null);
  assert.ok(bakeStaffGlow(art, '#5972ff', 4).aura);
});

test('draw order: behind => staff first, hidden => body only', () => {
  const calls = [];
  const ctx = {
    save() {}, restore() {}, translate() {}, rotate() {}, getTransform: () => ({ a: 4, b: 0, c: 0, d: 4 }),
    drawImage: () => calls.push('staff'),
  };
  const body = () => calls.push('body');
  const baked = { body: { width: 10, height: 10 }, aura: null, gripX: 5, gripY: 5 };
  const held = (anchor) => ({ body: {}, layers: [{ anchor, baked }] });
  drawHeldHero(ctx, held({ x: 1, y: 1, angle: 0, s: 0.5, behind: true }), body, 0, 0, 24, 0);
  assert.deepEqual(calls.splice(0), ['staff', 'body']);
  drawHeldHero(ctx, held({ x: 1, y: 1, angle: 0, s: 0.5, behind: false }), body, 0, 0, 24, 0);
  assert.deepEqual(calls.splice(0), ['body', 'staff']);
  drawHeldHero(ctx, held({ hidden: true }), body, 0, 0, 24, 0);
  assert.deepEqual(calls.splice(0), ['body']);
  assert.ok(Object.keys(HELD_BARE_ANIMATIONS).length === 8);
});
