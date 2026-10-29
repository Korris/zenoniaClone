// Which world renderer runs: the classic Canvas2D one (default) or the PixiJS WebGL one. Chosen by
// `?renderer=pixi|canvas` (also remembered in localStorage) and flipped at runtime with F9, which reloads
// the page because the 2D canvas context is created with a different alpha mode per renderer.
// Pure functions only (storage and location are injected) so they are unit-testable without a DOM.

export const RENDERER_STORAGE_KEY = 'zenonia.renderer';
export const RENDERER_MODES = Object.freeze(['canvas', 'pixi']);
export const DEFAULT_RENDERER = 'canvas';

/** Normalised mode name, or null for anything unrecognised. */
export function parseRendererMode(value) {
  const mode = typeof value === 'string' ? value.trim().toLowerCase() : '';
  return RENDERER_MODES.includes(mode) ? mode : null;
}

/** Query string wins over the stored choice; both fall back to the default. */
export function resolveRendererMode(search = '', stored = null) {
  let queried = null;
  try { queried = new URLSearchParams(search).get('renderer'); } catch { /* malformed search string: ignore */ }
  return parseRendererMode(queried) || parseRendererMode(stored) || DEFAULT_RENDERER;
}

export const otherRendererMode = (mode) => (mode === 'pixi' ? 'canvas' : 'pixi');

/** `search` with `renderer=<mode>` set (other params kept). */
export function searchWithRenderer(search, mode) {
  const params = new URLSearchParams(search || '');
  params.set('renderer', mode);
  return `?${params.toString()}`;
}

function readStored(storage) {
  try { return storage?.getItem(RENDERER_STORAGE_KEY) ?? null; } catch { return null; }
}

/** Current mode for a browser window; remembers an explicit `?renderer=` choice. */
export function readRendererMode(win = globalThis) {
  const mode = resolveRendererMode(win.location?.search || '', readStored(win.localStorage));
  try { win.localStorage?.setItem(RENDERER_STORAGE_KEY, mode); } catch { /* storage blocked: the query still works */ }
  return mode;
}

/** F9: remember the other mode and reload into it. */
export function switchRendererMode(win = globalThis) {
  const next = otherRendererMode(readRendererMode(win));
  try { win.localStorage?.setItem(RENDERER_STORAGE_KEY, next); } catch { /* ignore */ }
  win.location.search = searchWithRenderer(win.location.search, next);
  return next;
}
