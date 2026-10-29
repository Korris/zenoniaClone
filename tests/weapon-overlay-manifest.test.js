import test from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { WEAPON_OVERLAYS } from '../src/data/weapon-overlay-manifest-generated.js';
import { WEAPON_TEMPLATES } from '../src/data/item-definitions.js';

const dir = new URL('../assets/custom/weapon-overlays/', import.meta.url);

test('overlay manifest covers every weapon template', () => {
  assert.deepEqual(Object.keys(WEAPON_OVERLAYS).sort(), Object.keys(WEAPON_TEMPLATES).sort());
});

for (const [type, entry] of Object.entries(WEAPON_OVERLAYS)) {
  test(`overlay ${type}: fields, grip bounds and PNG headers`, () => {
    assert.match(entry.glow, /^#[0-9a-f]{6}$/);
    assert.ok(entry.length >= 0.6 && entry.length <= 1.4, `length ${entry.length}`);
    for (const tag of ['base', 'enhanced']) {
      const m = entry[tag];
      assert.ok(m.w > 0 && m.h > 0);
      assert.ok(m.gripX >= 0 && m.gripX < m.w && m.gripY >= 0 && m.gripY < m.h, `${tag} grip in bounds`);
      const file = new URL(m.file, dir);
      assert.ok(fs.existsSync(file), `${m.file} exists`);
      const head = fs.readFileSync(file).subarray(0, 24);
      assert.equal(head.toString('latin1', 1, 4), 'PNG');
      assert.equal(head.readUInt32BE(16), m.w);
      assert.equal(head.readUInt32BE(20), m.h);
    }
  });
}
