import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { WIZARD_STAFF_OVERLAYS } from '../src/data/wizard-staff-overlay-manifest-generated.js';

const root = new URL('../', import.meta.url);

test('wizard staff manifest has 10 tiers with unique names', () => {
  assert.equal(WIZARD_STAFF_OVERLAYS.length, 10);
  assert.equal(new Set(WIZARD_STAFF_OVERLAYS.map((e) => e.name)).size, 10);
});

WIZARD_STAFF_OVERLAYS.forEach((entry, tier) => {
  test(`wizard staff tier ${tier} ${entry.name}: glow, grip bounds and PNG headers`, () => {
    assert.match(entry.glow, /^#[0-9a-f]{6}$/);
    for (const tag of ['base', 'enhanced']) {
      const m = entry[tag];
      assert.ok(m.w > 0 && m.h > 0);
      assert.ok(m.gripX >= 0 && m.gripX < m.w && m.gripY >= 0 && m.gripY < m.h, `${tag} grip in bounds`);
      const file = new URL(m.src, root);
      assert.ok(fs.existsSync(file), `${m.src} exists`);
      const head = fs.readFileSync(file).subarray(0, 24);
      assert.equal(head.toString('latin1', 1, 4), 'PNG');
      assert.equal(head.readUInt32BE(16), m.w);
      assert.equal(head.readUInt32BE(20), m.h);
    }
  });
});
