// Loads the image files listed in the asset atlas before the game starts.
import { ASSET_FILES } from './asset-atlas.js';

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
 * Loads every file in ASSET_FILES. A failed load (e.g. an HD sheet not generated yet) does NOT
 * reject the whole boot -- it's logged as a warning and simply dropped from the returned map, so
 * callers (sprite-registry.js) must treat any `images[key]` as possibly undefined.
 * @param {(loaded:number, total:number) => void} onProgress optional progress callback
 * @returns {Promise<Record<string, HTMLImageElement>>} images keyed by atlas name (missing keys omitted)
 */
export async function loadAllImages(onProgress = null) {
  const entries = Object.entries(ASSET_FILES);
  const images = {};
  let loaded = 0;
  const results = await Promise.allSettled(entries.map(([, path]) => loadImage(path)));
  results.forEach((result, i) => {
    const [key, path] = entries[i];
    if (result.status === 'fulfilled') {
      images[key] = result.value;
    } else {
      console.warn(`[asset-loader] missing asset "${key}" (${path}): ${result.reason?.message || result.reason}`);
    }
    loaded++;
    if (onProgress) onProgress(loaded, entries.length);
  });
  return images;
}
