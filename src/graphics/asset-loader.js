// Loads image files listed in the asset atlas: the shared set before the title screen and, later, one class set.
import { SHARED_ASSET_FILES } from './asset-atlas.js';

const ASSET_ROOT = 'assets/';

function loadImage(path) {
  return new Promise((resolve, reject) => {
    const img = new Image();
    img.onload = () => resolve(img);
    img.onerror = () => reject(new Error(`Failed to load image: ${path}`));
    img.src = encodeURI(ASSET_ROOT + path); // some filenames (icons) contain spaces/& characters
  });
}

/**
 * Loads the files of `files` (key -> path) that `images` does not hold yet, adding them in place. A failed load
 * (e.g. an HD sheet not generated yet) does NOT reject: it is logged as a warning and the key is simply absent,
 * so callers (sprite builders) must treat any `images[key]` as possibly undefined.
 * @param {Record<string,string>} files image key -> path under assets/
 * @param {Record<string, HTMLImageElement>} images accumulating map (shared + loaded class images)
 * @param {(loaded:number, total:number) => void} onProgress optional progress callback
 * @param {(path:string) => Promise<HTMLImageElement>} loader injectable for tests
 * @returns {Promise<Record<string, HTMLImageElement>>} the same `images` map
 */
export async function loadImageSet(files, images = {}, onProgress = null, loader = loadImage) {
  const entries = Object.entries(files).filter(([key]) => !images[key]);
  let loaded = 0;
  await Promise.all(entries.map(([key, path]) => loader(path).then(
    (img) => { images[key] = img; },
    (reason) => console.warn(`[asset-loader] missing asset "${key}" (${path}): ${reason?.message || reason}`),
  ).then(() => {
    loaded++;
    if (onProgress) onProgress(loaded, entries.length);
  })));
  return images;
}

/** The shared set (everything except class-owned art). */
export function loadSharedImages(onProgress = null) {
  return loadImageSet(SHARED_ASSET_FILES, {}, onProgress);
}
