// Loads PixiJS (and pixi-filters for the bloom) on demand. The bare specifiers 'pixi.js' / 'pixi-filters' are
// resolved by the import map in index.html, which points at the vendored builds (see README "Vendored libs"),
// so the Canvas2D renderer never downloads or parses them.

let loading = null;

/** @returns {Promise<{PIXI: object, filters: object|null}>} filters is null when the bloom module is unavailable. */
export function loadPixi() {
  loading ||= (async () => {
    const PIXI = await import('pixi.js');
    let filters = null;
    try { filters = await import('pixi-filters'); } catch (err) { console.warn('pixi-filters unavailable, bloom disabled', err); }
    return { PIXI, filters };
  })();
  return loading;
}
