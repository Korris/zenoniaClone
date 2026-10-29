// Lazy per-class art: which files belong to the shared boot set vs. each class, and the loader's dedupe / fallback rules.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { SHARED_ASSET_FILES, ASSET_FILES, classAssetFiles } from '../src/graphics/asset-atlas.js';
import { CLASS_IDS_WITH_ART, ALL_CLASS_ASSET_FILES, HD_CLASS_SHEETS } from '../src/graphics/asset-atlas-class-files.js';
import { createClassAssetLoader } from '../src/graphics/class-asset-loader.js';
import { loadImageSet } from '../src/graphics/asset-loader.js';
import { CLASS_IDS } from '../src/data/class-definitions.js';

const keysOf = (classId) => new Set(Object.keys(classAssetFiles(classId)));

test('every playable class has an asset set and the art-class list matches class-definitions', () => {
  assert.deepEqual([...CLASS_IDS].sort(), [...CLASS_IDS_WITH_ART].sort());
  for (const id of CLASS_IDS) assert.ok(Object.keys(classAssetFiles(id)).length > 20, id);
  assert.deepEqual(classAssetFiles('hacker'), {});
});

test('a class set holds its own hero sheets, held gear and roster -- and nothing of another class', () => {
  const wizard = keysOf('wizard'), mechanic = keysOf('mechanic'), cleric = keysOf('cleric');
  for (const key of ['hdWizardIdle', 'hdWizardBareIdle', 'wizardStaff0base', 'paintedSkillswizard', 'iceStorm', 'hdFxMagicBolt']) assert.ok(wizard.has(key), key);
  for (const key of ['hdMechanicIdle', 'mechanicGun0base', 'mechanicRobot0base', 'paintedSkillsmechanic', 'rocketBarrage', 'hdFxBullet']) assert.ok(mechanic.has(key), key);
  for (const key of ['hdClericIdle', 'clericSpirit0base', 'clericSpiritFloatdown0']) assert.ok(cleric.has(key), key);
  assert.ok(keysOf('warrior').has('earthBreak_slam_3'));
  assert.ok(keysOf('assassin').has('assassinDagger0base'));
  for (const id of CLASS_IDS) {
    for (const other of CLASS_IDS.filter((c) => c !== id)) {
      const shared = [...keysOf(id)].filter((k) => keysOf(other).has(k));
      assert.deepEqual(shared.filter((k) => k !== 'ultimateIcons'), [], `${id}/${other} share only the ultimate-icon fallback sheet`);
    }
  }
});

test('shared boot set excludes class art but keeps the hero-select portraits and the title warrior idle', () => {
  for (const key of Object.keys(ALL_CLASS_ASSET_FILES)) {
    const isPreview = Object.values(HD_CLASS_SHEETS).some((s) => s.faceKey === key) || key === 'hdHeroIdle';
    assert.equal(key in SHARED_ASSET_FILES, isPreview, key);
  }
  assert.ok(Object.keys(SHARED_ASSET_FILES).length < Object.keys(ASSET_FILES).length * 0.5, 'less than half of the old boot set');
  assert.deepEqual({ ...SHARED_ASSET_FILES, ...ALL_CLASS_ASSET_FILES }, { ...ASSET_FILES });
});

test('every listed file exists on disk', () => {
  for (const [key, path] of Object.entries(ASSET_FILES)) assert.ok(fs.existsSync(`assets/${path}`), `${key}: ${path}`);
});

test('loadImageSet skips keys already loaded and tolerates failures', async () => {
  const requested = [];
  const loader = async (path) => { requested.push(path); if (path === 'bad.png') throw new Error('404'); return { path }; };
  const images = { a: { path: 'a.png' } };
  const progress = [];
  const warn = console.warn; console.warn = () => {};
  try { await loadImageSet({ a: 'a.png', b: 'b.png', c: 'bad.png' }, images, (l, t) => progress.push([l, t]), loader); } finally { console.warn = warn; }
  assert.deepEqual(requested, ['b.png', 'bad.png']);
  assert.deepEqual(Object.keys(images).sort(), ['a', 'b']);
  assert.deepEqual(progress.at(-1), [2, 2]);
});

function fakeRegistry() {
  return { hero: { base: true }, heroes: {}, loadedClasses: new Set() };
}

test('loader fetches only the chosen class once and registers it', async () => {
  const registry = fakeRegistry(), images = {};
  const fetched = [], registered = [];
  const loadSet = async (files) => { fetched.push(Object.keys(files)); };
  const loader = createClassAssetLoader(registry, images, { loadSet, register: (r, i, id) => { registered.push(id); r.loadedClasses.add(id); } });
  const [a, b] = await Promise.all([loader.ensure('wizard'), loader.ensure('wizard')]);
  assert.deepEqual([a.ok, b.ok], [true, true]);
  assert.equal(fetched.length, 1, 'one fetch for concurrent ensure() calls');
  assert.deepEqual(new Set(fetched[0]), keysOf('wizard'));
  assert.deepEqual(registered, ['wizard']);
  assert.equal(loader.isLoaded('wizard'), true);
  assert.equal(loader.isLoaded('mechanic'), false);
  await loader.ensure('wizard');
  assert.equal(fetched.length, 1, 'already loaded => no refetch');
});

test('a class whose art fails to build falls back to the base hero instead of rejecting', async () => {
  const registry = fakeRegistry();
  const err = console.error; console.error = () => {};
  try {
    const loader = createClassAssetLoader(registry, {}, { loadSet: async () => {}, register: () => { throw new Error('bad sheet'); } });
    const result = await loader.ensure('cleric');
    assert.equal(result.ok, false);
  } finally { console.error = err; }
  assert.equal(registry.heroes.cleric, registry.hero);
  assert.equal(registry.loadedClasses.has('cleric'), true, 'game proceeds');
});

test('unknown class id fetches nothing', async () => {
  let called = false;
  const loader = createClassAssetLoader(fakeRegistry(), {}, { loadSet: async () => { called = true; }, register: () => {} });
  assert.equal((await loader.ensure('hacker')).ok, false);
  assert.equal(called, false);
});
