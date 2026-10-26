// Slices the HD monster walk-cycle sheets (asset-atlas-hd-monsters.js) into per-facing frame lists,
// mirroring sprite-registry.js's buildNpcsHd(). Split out to keep sprite-registry.js under 200 lines.
import { cropSprite } from './sprite-cell.js';
import { createTintedSprite, HIT_FLASH_COLOR } from './pixel-sprite-factory.js';
import { HD_MONSTER, HD_MONSTERS, monsterSheetKey } from './asset-atlas-hd-monsters.js';

/**
 * @param {Record<string, HTMLImageElement>} images loaded images keyed like ASSET_FILES
 * @returns {Record<string, {frames: Record<string, HTMLCanvasElement[]>, hitFlash: Record<string, HTMLCanvasElement>, bodyUnits: number}>}
 *   keyed by monster type -- only types whose sheet loaded (monster-renderer.js falls back to the
 *   16px pixel sheet for the rest). `bodyUnits` = painted body height in world units.
 */
export function buildMonstersHd(images) {
  const { frame, pixelsPerUnit, cols, rows } = HD_MONSTER;
  const monstersHd = {};
  for (const [type, { bodyHeight }] of Object.entries(HD_MONSTERS)) {
    const img = images[monsterSheetKey(type)];
    if (!img) continue; // not generated yet -- see tools/build-hd-monsters.py
    const frames = {};
    const hitFlash = {};
    rows.forEach((dir, row) => {
      frames[dir] = [];
      for (let col = 0; col < cols; col++) {
        frames[dir].push(cropSprite(img, col * frame, row * frame, frame, frame, false, pixelsPerUnit));
      }
      hitFlash[dir] = createTintedSprite(frames[dir][0], HIT_FLASH_COLOR);
    });
    monstersHd[type] = { frames, hitFlash, bodyUnits: bodyHeight / pixelsPerUnit };
  }
  return monstersHd;
}
