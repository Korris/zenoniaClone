// Shared by held gear (held-weapon-registry.js) and companions (companion-renderer.js): turns a generated overlay
// manifest + loaded images into per-tier art records.

/**
 * @param {Record<string, HTMLImageElement>} images loaded atlas images
 * @param {object[]} manifest overlay manifest (index = equipment tier; entries carry base/enhanced grips + glow)
 * @param {(tier:number, variant:'base'|'enhanced') => string} imageKey atlas key of a tier's art
 * @returns {Array<{base:{img,gripX,gripY}, enhanced:{img,gripX,gripY}, glow:object}|undefined>} sparse: tiers whose art did not load are holes
 */
export function buildOverlayList(images, manifest, imageKey) {
  const overlays = [];
  manifest.forEach((entry, tier) => {
    const base = images[imageKey(tier, 'base')], enhanced = images[imageKey(tier, 'enhanced')];
    if (!base || !enhanced) return;
    overlays[tier] = {
      base: { img: base, gripX: entry.base.gripX, gripY: entry.base.gripY },
      enhanced: { img: enhanced, gripX: entry.enhanced.gripX, gripY: entry.enhanced.gripY },
      glow: entry.glow,
    };
  });
  return overlays;
}
