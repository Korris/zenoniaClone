// Glue between the game controller and the PixiJS world view: the controller calls backend.renderWorld() where it
// would call renderWorld(); the WebGL scene is drawn first, then the Canvas2D leftovers (legacy overlay) go onto the
// transparent 2D canvas, and the controller finally draws the HUD over everything.
import { PixiWorldView } from './pixi-world-view.js';
import { drawLegacyOverlay } from './pixi-legacy-overlay.js';

/** @param {HTMLCanvasElement} canvas2d the game's 2D canvas (the WebGL canvas is inserted just below it) */
export async function createPixiBackend(canvas2d) {
  const view = await PixiWorldView.create(canvas2d);
  const renderWorld = (ctx, world, registry, info = {}) => {
    view.render(world, registry, info);
    drawLegacyOverlay(ctx, world, registry, info, view.status);
  };
  return { view, renderWorld };
}
