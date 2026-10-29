// Boot-time wiring of held gear: pairs each baked hero frame with its gear-less body twin and the per-part
// anchors. Anything missing (sheet, frame count, anchor, overlay art) => that animation is left out and the
// renderer keeps drawing the baked frame (zero regression).
import { HELD_WEAPON_ENABLED } from './held-weapon-config.js';
import { HELD_GEAR_SPECS } from './held-gear-specs.js';
import { buildOverlayList } from './held-overlay-builder.js';

const DIRS = ['down', 'up', 'left', 'right'];

const framesReady = (set, anim, n) => DIRS.every((dir) => set?.[dir]?.[anim]?.length === n);
const anchorsReady = (anchors, anim, n) => DIRS.every((dir) => anchors?.[anim]?.[dir]?.length === n);

/**
 * @param {object} images loaded atlas images
 * @param {string} classId key of HELD_GEAR_SPECS
 * @param {object} classSet registry.heroes[classId] ({dir:{anim:[frame]}}), the baked frames
 * @param {Function} buildHeroHd sheet slicer from sprite-registry.js (same row/mirror rules as the baked set)
 * @param {object} sideRows side-row table of the class sheets
 * @returns {{byFrame: WeakMap<object,{bare,partial,anchors}>, overlays: object, parts: object[]} | null}
 */
export function buildHeldGear(images, classId, classSet, buildHeroHd, sideRows, spec = HELD_GEAR_SPECS[classId]) {
  if (!HELD_WEAPON_ENABLED || !spec) return null;
  const overlays = {};
  for (const part of spec.parts) overlays[part.id] = buildOverlayList(images, part.manifest, part.imageKey);
  if (spec.parts.some((p) => !p.optional && !overlays[p.id].length)) return null;
  const bare = buildHeroHd(images, spec.bare, sideRows);
  const partial = spec.partial ? buildHeroHd(images, spec.partial, sideRows) : null;
  const byFrame = new WeakMap();
  for (const anim of Object.keys(spec.bare)) {
    const n = classSet.down?.[anim]?.length;
    if (!n || !framesReady(classSet, anim, n) || !framesReady(bare, anim, n)) continue;
    if (spec.parts.some((p) => !p.optional && !anchorsReady(p.anchors, anim, n))) continue;
    const usePartial = partial && framesReady(partial, anim, n);
    for (const dir of DIRS) {
      classSet[dir][anim].forEach((frame, i) => {
        const anchors = {};
        for (const p of spec.parts) if (anchorsReady(p.anchors, anim, n) && overlays[p.id].length) anchors[p.id] = p.anchors[anim][dir][i];
        byFrame.set(frame, { bare: bare[dir][anim][i], partial: usePartial ? partial[dir][anim][i] : null, anchors });
      });
    }
  }
  return { byFrame, overlays, parts: spec.parts };
}
