// Lazy per-class art: after the hero is picked (New Game) or read from the save (Continue) only that class's image set
// is fetched and its sprites registered. Failure policy: a missing image is tolerated by the loader (builders fall
// back); if building still throws, the class falls back to the shared base hero so the game never stays on a loading screen.
import { classAssetFiles } from './asset-atlas-class-files.js';
import { loadImageSet } from './asset-loader.js';
import { registerClassSprites } from './class-sprite-builder.js';

/**
 * @param {object} registry from buildSpriteRegistry
 * @param {Record<string, HTMLImageElement>} images the shared image map (class images are added to it)
 * @param {{loadSet?: Function, register?: Function}} deps injectable for tests
 * @returns {{ensure: (classId:string, onProgress?:Function) => Promise<{ok:boolean}>, isLoaded: (classId:string) => boolean}}
 */
export function createClassAssetLoader(registry, images, { loadSet = loadImageSet, register = registerClassSprites } = {}) {
  const pending = new Map(); // classId -> Promise (one fetch per class, even if ensure() is called twice)

  async function load(classId, onProgress) {
    const files = classAssetFiles(classId);
    if (!Object.keys(files).length) return { ok: false }; // unknown class id: nothing to fetch
    try {
      await loadSet(files, images, onProgress);
      register(registry, images, classId);
      return { ok: true };
    } catch (err) {
      console.error(`[class-assets] ${classId} art failed, using the base hero:`, err);
      registry.heroes[classId] = registry.heroes[classId] || registry.hero;
      registry.loadedClasses.add(classId);
      return { ok: false };
    }
  }

  return {
    ensure(classId, onProgress = null) {
      if (registry.loadedClasses.has(classId)) return Promise.resolve({ ok: true });
      if (!pending.has(classId)) {
        pending.set(classId, load(classId, onProgress).finally(() => pending.delete(classId)));
      }
      return pending.get(classId);
    },
    isLoaded: (classId) => registry.loadedClasses.has(classId),
  };
}
